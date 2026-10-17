import { HttpResponse, http, type RequestHandler } from "msw";
import type { AuthenticationErrorCode } from "@vitarish/auth";

type MockAuthHandlerContext = {
  isUnavailable: boolean;
  withDelay: () => Promise<void>;
  unavailable: () => Response;
};

/** Password the mock sign-in rejects as invalid credentials; any other password asks for a second step. */
export const mockRejectedPassword = "wrong-password";

/** Values match the Authentication module's AuthenticationChallengePurposeDto enum. */
const challengePurpose = {
  phoneLogin: 0,
  emailLogin: 1,
  passwordLoginOtp: 3,
  emailPasswordRecovery: 6,
} as const;

const challengeLifetimeMilliseconds = 5 * 60_000;
const challengeAttempts = 5;
const authenticationPath = "\\/api\\/modules\\/authentication\\/";
const route = (path: string) => new RegExp(`${authenticationPath}${path}(?:\\?|$)`);

const challenge = (purpose: number) => ({
  challengeId: `mock-challenge-${purpose}`,
  purpose,
  expiresAt: new Date(Date.now() + challengeLifetimeMilliseconds).toISOString(),
  remainingAttempts: challengeAttempts,
});

const authFailure = (status: number, code: AuthenticationErrorCode, challengeId?: string) =>
  HttpResponse.json({ status, code, title: "Mock authentication failure", challengeId }, { status });

/** Sign-in, password recovery, and sign-out answers so each authentication screen and step can be reviewed. */
export function createAuthMockHandlers({ isUnavailable, withDelay, unavailable }: MockAuthHandlerContext): RequestHandler[] {
  const answer = (respond: (request: Request) => Promise<Response> | Response) => async ({ request }: { request: Request }) => {
    await withDelay();
    return isUnavailable ? unavailable() : respond(request);
  };

  return [
    http.post(route("login\\/password"), answer(async (request) => {
      const body = await request.json().catch(() => null) as { password?: unknown } | null;
      return body?.password === mockRejectedPassword
        ? authFailure(401, "invalid_credentials")
        : authFailure(401, "additional_step_required", challenge(challengePurpose.passwordLoginOtp).challengeId);
    })),
    http.post(route("login\\/phone\\/begin"), answer(() => HttpResponse.json(challenge(challengePurpose.phoneLogin)))),
    http.post(route("login\\/email\\/begin"), answer(() => HttpResponse.json(challenge(challengePurpose.emailLogin)))),
    http.post(route("password-recovery\\/begin"), answer(() => HttpResponse.json(challenge(challengePurpose.emailPasswordRecovery)))),
    http.post(route("password-recovery\\/complete"), answer(() => HttpResponse.json({ succeeded: true }))),
    http.get(route("logout\\/context"), answer(() => HttpResponse.json({
      logoutId: null,
      clientName: null,
      postLogoutRedirectUri: null,
      showLogoutPrompt: true,
    }))),
  ];
}

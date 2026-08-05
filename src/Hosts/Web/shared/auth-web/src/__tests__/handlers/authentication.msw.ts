import { http, HttpResponse } from "msw";
import type {
  AuthenticationRedirectResultDto,
  AuthenticationChallengeDto,
  AuthenticationSessionDto,
  LogoutResultDto,
  LogoutContextDto,
} from "../../api/generated/authentication/model";

const apiPath = (path: string) => new RegExp(`/api/modules/authentication${path.replaceAll("/", "\\/")}(?:\\?|$)`);

export const handlers = [
  http.post(apiPath("/login/password"), () => {
    const body: AuthenticationRedirectResultDto = {
      returnUrl: "/home",
      requiresProfileCompletion: false,
    };
    return HttpResponse.json(body);
  }),

  http.post(apiPath("/login/phone/begin"), () => {
    const body: AuthenticationChallengeDto = {
      challengeId: "test-challenge-id",
      purpose: 1,
      expiresAt: new Date(Date.now() + 5 * 60 * 1000).toISOString(),
      remainingAttempts: 5,
    };
    return HttpResponse.json(body);
  }),

  http.post(apiPath("/login/phone/complete"), () => {
    const body: AuthenticationRedirectResultDto = {
      returnUrl: "/home",
      requiresProfileCompletion: false,
    };
    return HttpResponse.json(body);
  }),

  http.post(apiPath("/login/email/begin"), () => {
    const body: AuthenticationChallengeDto = {
      challengeId: "test-challenge-id",
      purpose: 2,
      expiresAt: new Date(Date.now() + 5 * 60 * 1000).toISOString(),
      remainingAttempts: 5,
    };
    return HttpResponse.json(body);
  }),

  http.post(apiPath("/login/email/complete"), () => {
    const body: AuthenticationRedirectResultDto = {
      returnUrl: "/home",
      requiresProfileCompletion: false,
    };
    return HttpResponse.json(body);
  }),

  http.post(apiPath("/login/passkey/begin"), () => {
    const body: AuthenticationChallengeDto = {
      challengeId: "test-challenge-id",
      purpose: 3,
      expiresAt: new Date(Date.now() + 60 * 1000).toISOString(),
      remainingAttempts: 1,
      optionsJson: JSON.stringify({
        challenge: new Uint8Array(32),
        timeout: 60000,
        rpId: "localhost",
        allowCredentials: [],
        userVerification: "preferred",
      }),
    };
    return HttpResponse.json(body);
  }),

  http.post(apiPath("/login/passkey/complete"), () => {
    const body: AuthenticationRedirectResultDto = {
      returnUrl: "/home",
      requiresProfileCompletion: false,
    };
    return HttpResponse.json(body);
  }),

  http.get(apiPath("/session"), () => {
    const body: AuthenticationSessionDto = {
      isAuthenticated: false,
      user: null,
      canAccessPlatformAdmin: false,
      tenantAccess: { canAccessTenantOwner: false, canAccessTenantUser: false },
    };
    return HttpResponse.json(body);
  }),

  http.get(apiPath("/me"), () => {
    return HttpResponse.json(null, { status: 404 });
  }),

  http.get(apiPath("/logout/context"), () => {
    const body: LogoutContextDto = {
      logoutId: null,
      clientName: null,
      postLogoutRedirectUri: null,
      showLogoutPrompt: false,
    };
    return HttpResponse.json(body);
  }),

  http.post(apiPath("/logout"), () => {
    const body: LogoutResultDto = {
      signedOut: true,
      redirectUrl: "/login",
    };
    return HttpResponse.json(body);
  }),

  http.get(apiPath("/antiforgery"), () => {
    return HttpResponse.json({
      requestToken: "test-antiforgery-token",
      headerName: "X-XSRF-TOKEN",
    });
  }),
];

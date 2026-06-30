import { http, HttpResponse } from "msw";
import type {
  AuthenticationRedirectResultDto,
  AuthenticationChallengeDto,
  AuthenticationSessionDto,
  LogoutResultDto,
  LogoutContextDto,
} from "@/api/generated/authentication/model";

const BASE = "https://localhost:7272";

export const handlers = [
  http.post(`${BASE}/api/modules/authentication/login/password`, () => {
    const body: AuthenticationRedirectResultDto = {
      returnUrl: "/home",
      requiresProfileCompletion: false,
    };
    return HttpResponse.json(body);
  }),

  http.post(`${BASE}/api/modules/authentication/login/phone/begin`, () => {
    const body: AuthenticationChallengeDto = {
      challengeId: "test-challenge-id",
      purpose: 1,
      expiresAt: new Date(Date.now() + 5 * 60 * 1000).toISOString(),
      remainingAttempts: 5,
    };
    return HttpResponse.json(body);
  }),

  http.post(`${BASE}/api/modules/authentication/login/phone/complete`, () => {
    const body: AuthenticationRedirectResultDto = {
      returnUrl: "/home",
      requiresProfileCompletion: false,
    };
    return HttpResponse.json(body);
  }),

  http.post(`${BASE}/api/modules/authentication/login/email/begin`, () => {
    const body: AuthenticationChallengeDto = {
      challengeId: "test-challenge-id",
      purpose: 2,
      expiresAt: new Date(Date.now() + 5 * 60 * 1000).toISOString(),
      remainingAttempts: 5,
    };
    return HttpResponse.json(body);
  }),

  http.post(`${BASE}/api/modules/authentication/login/email/complete`, () => {
    const body: AuthenticationRedirectResultDto = {
      returnUrl: "/home",
      requiresProfileCompletion: false,
    };
    return HttpResponse.json(body);
  }),

  http.post(`${BASE}/api/modules/authentication/login/passkey/begin`, () => {
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

  http.post(`${BASE}/api/modules/authentication/login/passkey/complete`, () => {
    const body: AuthenticationRedirectResultDto = {
      returnUrl: "/home",
      requiresProfileCompletion: false,
    };
    return HttpResponse.json(body);
  }),

  http.get(`${BASE}/api/modules/authentication/session`, () => {
    const body: AuthenticationSessionDto = {
      isAuthenticated: false,
      user: null,
    };
    return HttpResponse.json(body);
  }),

  http.get(`${BASE}/api/modules/authentication/me`, () => {
    return HttpResponse.json(null, { status: 404 });
  }),

  http.get(`${BASE}/api/modules/authentication/logout/context`, () => {
    const body: LogoutContextDto = {
      logoutId: null,
      clientName: null,
      postLogoutRedirectUri: null,
      showLogoutPrompt: false,
    };
    return HttpResponse.json(body);
  }),

  http.post(`${BASE}/api/modules/authentication/logout`, () => {
    const body: LogoutResultDto = {
      signedOut: true,
      redirectUrl: "/login",
    };
    return HttpResponse.json(body);
  }),

  http.get(`${BASE}/api/modules/authentication/antiforgery`, () => {
    return HttpResponse.json({
      requestToken: "test-antiforgery-token",
      headerName: "X-XSRF-TOKEN",
    });
  }),
];

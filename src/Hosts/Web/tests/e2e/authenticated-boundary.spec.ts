import { expect, test, type APIRequestContext, type Page } from "@playwright/test";
import { existsSync, mkdirSync, readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { usersGetProfile } from "../../shared/auth/src/api/generated/users/users";
import {
  authenticationAccessHistoryGet,
  authenticationLogoutPost,
  authenticationSessionGet,
  authenticationSessionNoticesGet,
} from "../../shared/auth/src/api/generated/authentication/authentication";
import { auditPlatformSecurityEventsList, auditSecurityEventsList, getAuditPlatformSecurityEventCatalogGetQueryKey, getAuditPlatformSecurityEventsExportQueryKey } from "../../shared/security/src/api/generated/audit/audit";
import type { AuditPlatformSecurityEventsListParams, SecurityAuditPageReadDto } from "../../shared/security/src/api/generated/audit/model";
import { auditSortFields } from "../../shared/security/src/api/securityAuditSortFields";
import { ApiError, ANTIFORGERY_VALIDATION_FAILED_CODE, TENANT_HOST_HEADER } from "../../shared/api-client/src/apiFetch";
import { localizePlatformAuditCategory, localizePlatformAuditEvent } from "../../shared/security/src/localization";
import { Routes as ClinicRoutes } from "../../apps/private-practice-owner/src/app/routes";
import { twoHostE2eApiOriginEnvironmentKey, twoHostE2ePlatformAdminClientIp, twoHostE2eProofScreenshotEnvironmentKey, twoHostE2eTenantBClinicOrigin, twoHostE2eTenantBOwnerUsername, twoHostE2eTenantBOwnerUsernameEnvironmentKey } from "../../scripts/two-host-e2e-artifacts.mjs";
import { agentEnvelopeCanonical, base64UrlHmac } from "./agentCallback";
import { forwardedForHeaderName, signInToClinic } from "./signInHost";

// Main itself, for API-boundary checks made without the browser.
const apiOrigin = process.env[twoHostE2eApiOriginEnvironmentKey] ?? "https://localhost:7272";
// In-page requests use the web origin's same-origin /api, as in production.
const browserApiOrigin = "";
const e2eUsername = process.env.VITARISH_E2E_USERNAME ?? "e2e-owner";
const e2ePassword = process.env.VITARISH_E2E_PASSWORD;
const webOrigin = process.env.VITARISH_E2E_WEB_ORIGIN ?? "http://localhost:3005";
const tenantUserUsername = process.env.VITARISH_E2E_TENANT_USER_USERNAME ?? "e2e-tenant-user";
const tenantBOwnerUsername = process.env[twoHostE2eTenantBOwnerUsernameEnvironmentKey] ?? twoHostE2eTenantBOwnerUsername;
const platformAdminUsername = process.env.VITARISH_E2E_PLATFORM_ADMIN_USERNAME ?? "e2e-platform-admin";
const lockoutPlatformAdminUsername = "e2e-lockout-platform-admin";
const tenantBOrigin = twoHostE2eTenantBClinicOrigin;
const platformAdminOrigin = process.env.PLAYWRIGHT_PLATFORM_ADMIN_ORIGIN ?? "http://localhost:3006";
const forbiddenScreenshotPath = process.env.VITARISH_E2E_FORBIDDEN_SCREENSHOT;
const tenantAId = "9c8b6b3a-7d6e-4f75-a88d-7d3f5b1b0f92";
const tenantAPatientId = "2d7a3eb9-1d47-4c63-9a45-19d7e6a0fb18";
const tenantBPatientId = "f4c8109a-6f22-4a31-9ab8-48d7ad0d7e2f";
const tenantBId = "b2d2e4a1-2fd8-4781-8d9f-6c96f2a5e7b4";
const tenantBOwnerUserId = "c65fcb80-8db3-4e2f-b3b9-1c4d7b1e0a62";
const tenantAFileId = "4b8d1e7a-9f24-4d65-a8b1-6c0e2f5d7a91";
const tenantBFileId = "9e2f6a41-7c35-4b8d-a0e6-1f5d9c2b7a84";
// Tenant B's seeded logo, which the shared sign-in host's clinic list shows (E2eFixtureSeeder).
const tenantBLogoFileId = "6d1e8b3f-2a94-4c57-b0d8-9f3a7e5c1b26";
const smtpCapturePath = process.env.VITARISH_E2E_SMTP_CAPTURE_PATH;
const agentSharedSecret = process.env.VITARISH_E2E_AGENT_SHARED_SECRET;

async function fetchJson(page: Page, path: string) {
  return page.evaluate(async ({ apiOrigin: origin, path: requestPath }) => {
    const response = await fetch(`${origin}${requestPath}`, {
      credentials: "include",
      headers: { "X-Vitarish-Tenant-Host": window.location.host },
    });
    let body: unknown = null;
    try {
      body = await response.json();
    } catch {
      // The status assertion below is the useful diagnostic for an empty response.
    }
    return { status: response.status, body };
  }, { apiOrigin: browserApiOrigin, path });
}

async function postJson(page: Page, path: string, body: unknown) {
  return page.evaluate(async ({ apiOrigin: origin, path: requestPath, body: requestBody }) => {
    const antiforgeryResponse = await fetch(`${origin}/api/modules/authentication/antiforgery`, {
      credentials: "include",
      headers: { "X-Vitarish-Tenant-Host": window.location.host },
    });
    const antiforgery = await antiforgeryResponse.json() as { headerName: string; requestToken: string };
    const response = await fetch(`${origin}${requestPath}`, {
      method: "POST",
      credentials: "include",
      headers: {
        "Content-Type": "application/json",
        "X-Vitarish-Tenant-Host": window.location.host,
        "X-Request-Id": `e2e-${crypto.randomUUID()}`,
        [antiforgery.headerName]: antiforgery.requestToken,
      },
      body: JSON.stringify(requestBody),
    });
    let responseBody: unknown = null;
    try {
      responseBody = await response.json();
    } catch {
      // Non-JSON responses are still represented by their status below.
    }
    return { status: response.status, body: responseBody };
  }, { apiOrigin: browserApiOrigin, path, body });
}

async function putJson(page: Page, path: string, body: unknown) {
  return page.evaluate(async ({ apiOrigin: origin, path: requestPath, body: requestBody }) => {
    const antiforgeryResponse = await fetch(`${origin}/api/modules/authentication/antiforgery`, {
      credentials: "include",
      headers: { "X-Vitarish-Tenant-Host": window.location.host },
    });
    const antiforgery = await antiforgeryResponse.json() as { headerName: string; requestToken: string };
    const response = await fetch(`${origin}${requestPath}`, {
      method: "PUT",
      credentials: "include",
      headers: {
        "Content-Type": "application/json",
        "X-Vitarish-Tenant-Host": window.location.host,
        "X-Request-Id": `e2e-${crypto.randomUUID()}`,
        [antiforgery.headerName]: antiforgery.requestToken,
      },
      body: JSON.stringify(requestBody),
    });
    let responseBody: unknown = null;
    try {
      responseBody = await response.json();
    } catch {
      // Non-JSON responses are still represented by their status below.
    }
    return { status: response.status, body: responseBody };
  }, { apiOrigin: browserApiOrigin, path, body });
}

async function fetchText(page: Page, path: string) {
  return page.evaluate(async ({ apiOrigin: origin, path: requestPath }) => {
    const response = await fetch(`${origin}${requestPath}`, {
      credentials: "include",
      headers: { "X-Vitarish-Tenant-Host": window.location.host },
    });
    return { status: response.status, body: await response.text() };
  }, { apiOrigin: browserApiOrigin, path });
}

function unsignedBearerToken(payload: Record<string, unknown>, header: Record<string, unknown> = { alg: "none", typ: "at+jwt" }) {
  const encode = (value: unknown) => Buffer.from(JSON.stringify(value)).toString("base64url");
  return `${encode(header)}.${encode(payload)}.`;
}


async function deleteJson(page: Page, path: string, body: unknown) {
  return page.evaluate(async ({ apiOrigin: origin, path: requestPath, body: requestBody }) => {
    const antiforgeryResponse = await fetch(`${origin}/api/modules/authentication/antiforgery`, {
      credentials: "include",
      headers: { "X-Vitarish-Tenant-Host": window.location.host },
    });
    const antiforgery = await antiforgeryResponse.json() as { headerName: string; requestToken: string };
    const response = await fetch(`${origin}${requestPath}`, {
      method: "DELETE",
      credentials: "include",
      headers: {
        "Content-Type": "application/json",
        "X-Vitarish-Tenant-Host": window.location.host,
        "X-Request-Id": `e2e-${crypto.randomUUID()}`,
        [antiforgery.headerName]: antiforgery.requestToken,
      },
      body: JSON.stringify(requestBody),
    });
    const responseText = await response.text();
    let responseBody: unknown = undefined;
    if (responseText) {
      try {
        responseBody = JSON.parse(responseText);
      } catch {
        responseBody = responseText;
      }
    }
    return { status: response.status, body: responseBody };
  }, { apiOrigin: browserApiOrigin, path, body });
}

async function generatedUserGet(request: APIRequestContext, userId: string, cookieHeader: string, tenantHost: string) {
  const nativeFetch = globalThis.fetch;
  globalThis.fetch = (async (input: RequestInfo | URL, init?: RequestInit) => {
    const headers = new Headers(init?.headers);
    const response = await request.get(input.toString(), {
      headers: Object.fromEntries(headers.entries()),
      ignoreHTTPSErrors: true,
    });
    const responseBody = await response.body();
    const body = responseBody.length > 0
      ? responseBody
      : JSON.stringify({ status: response.status(), title: response.statusText() });
    return new Response(body, {
      status: response.status(),
      headers: response.headers(),
    });
  }) as typeof fetch;

  try {
    return await usersGetProfile(userId, {
      headers: {
        Cookie: cookieHeader,
        "X-Vitarish-Tenant-Host": tenantHost,
      },
    });
  } finally {
    globalThis.fetch = nativeFetch;
  }
}

async function generatedAuthenticationRequest<T>(
  request: APIRequestContext,
  operation: () => Promise<T>,
  cookieHeader: string,
  tenantHost: string,
) {
  const nativeFetch = globalThis.fetch;
  let status = 0;
  globalThis.fetch = (async (input: RequestInfo | URL, init?: RequestInit) => {
    const headers = new Headers(init?.headers);
    headers.set("Cookie", cookieHeader);
    headers.set(TENANT_HOST_HEADER, tenantHost);
    const response = await request.fetch(input.toString(), {
      method: init?.method,
      headers: Object.fromEntries(headers.entries()),
      data: typeof init?.body === "string" ? init.body : undefined,
      ignoreHTTPSErrors: true,
    });
    status = response.status();
    const responseBody = await response.body();
    const body = responseBody.length > 0
      ? responseBody
      : JSON.stringify({ status: response.status(), title: response.statusText() });
    return new Response(body, {
      status: response.status(),
      headers: {
        "Content-Type": response.headers()["content-type"] ?? "application/json",
      },
    });
  }) as typeof fetch;

  try {
    const body = await operation();
    return { status, body };
  } catch (error) {
    if (error instanceof ApiError) return { status: error.status, body: error.problem };
    throw error;
  } finally {
    globalThis.fetch = nativeFetch;
  }
}

async function login(page: Page, username: string, origin = "", password = e2ePassword, clientIp = "198.51.100.10") {
  expect(password).toBeTruthy();
  await page.setExtraHTTPHeaders({ [forwardedForHeaderName]: clientIp });
  // Clinic hosts sign in only on the shared sign-in host; platform administration has its own sign-in.
  if (origin !== platformAdminOrigin) {
    await signInToClinic(page, username, password!, origin || webOrigin);
    return;
  }
  await page.goto(`${origin}/login/password`, { waitUntil: "domcontentloaded" });
  await expect(page.locator("form")).toHaveAttribute("method", "post", { timeout: 60_000 });
  await expect(page.locator('button[type="submit"]')).toBeEnabled({ timeout: 30_000 });
  await page.locator('input[name="username"]').fill(username);
  await page.locator('input[name="password"]').fill(password!);
  const [loginResponse] = await Promise.all([
    page.waitForResponse((response) => response.url().endsWith("/api/modules/authentication/login/password")),
    page.locator('button[type="submit"]').click(),
  ]);
  expect(loginResponse.status()).toBe(200);
}

test("uses the real Web and Main hosts for an authenticated tenant-scoped request", async ({ page, request }) => {
  const apiRequests: Array<{ method: string; url: string; tenantHost: string | undefined }> = [];
  const mockServiceWorkerRequests: string[] = [];
  page.on("request", (request) => {
    if (new URL(request.url()).pathname.startsWith("/api/")) {
      apiRequests.push({ method: request.method(), url: request.url(), tenantHost: request.headers()["x-vitarish-tenant-host"] });
    }
    if (request.url().includes("mockServiceWorker.js")) mockServiceWorkerRequests.push(request.url());
  });
  await login(page, e2eUsername, "", e2ePassword, "198.51.100.16");
  await page.goto("/home", { waitUntil: "domcontentloaded" });

  await expect(page).toHaveURL(/\/home(?:\?|$)/);
  await expect(page.locator("body")).not.toContainText("MSW");

  const session = await fetchJson(page, "/api/modules/authentication/session");
  expect(session.status).toBe(200);
  expect(session.body).toMatchObject({
    isAuthenticated: true,
    tenantAccess: { canAccessTenantOwner: true },
  });
  const browserCookies = await page.context().cookies();
  const cookieHeader = browserCookies.map(cookie => `${cookie.name}=${cookie.value}`).join("; ");
  const tenantHost = await page.evaluate(() => window.location.host);
  const accessHistory = await generatedAuthenticationRequest(
    request,
    () => authenticationAccessHistoryGet({ headers: { Cookie: cookieHeader, [TENANT_HOST_HEADER]: tenantHost } }),
    cookieHeader,
    tenantHost,
  );
  expect(accessHistory.status).toBe(200);
  expect(accessHistory.body).toMatchObject({
    lastSuccessfulAt: expect.any(String),
  });
  const sessionNotices = await generatedAuthenticationRequest(
    request,
    () => authenticationSessionNoticesGet({ headers: { Cookie: cookieHeader, [TENANT_HOST_HEADER]: tenantHost } }),
    cookieHeader,
    tenantHost,
  );
  expect(sessionNotices.status).toBe(200);
  expect(sessionNotices.body).toEqual(expect.any(Array));
  // The E2E Web host is next dev, where hydrating the dashboard takes several seconds.
  await expect(page.locator("#initial-page-loader")).toHaveCount(0, { timeout: 15_000 });
  await expect(page.getByRole("heading", { level: 1 })).toBeVisible({ timeout: 30_000 });

  const browserDisclosure = await page.evaluate((secret) => ({
    urlHasSecret: window.location.href.includes(secret),
    storageHasSecret: [
      ...Object.values(window.localStorage),
      ...Object.values(window.sessionStorage),
    ].some(value => value.includes(secret)),
  }), e2ePassword!);
  expect(browserDisclosure.urlHasSecret).toBe(false);
  expect(browserDisclosure.storageHasSecret).toBe(false);

  const antiforgery = await fetchJson(page, "/api/modules/authentication/antiforgery");
  expect(antiforgery.status).toBe(200);
  expect(antiforgery.body).toMatchObject({ headerName: "X-XSRF-TOKEN" });
  expect((antiforgery.body as { requestToken?: unknown }).requestToken).toEqual(expect.any(String));

  const health = await fetchJson(page, "/api/modules/appointments/health");
  expect(health.status).toBe(200);
  expect(health.body).toMatchObject({ succeeded: true });

  const removedAnonymousRoute = await fetchJson(page, "/__e2e/authenticate");
  expect(removedAnonymousRoute.status).toBe(404);

  await expect.poll(() => page.locator("body").innerText()).toMatch(/patients|patients/i);

  await page.goto("/settings/audit", { waitUntil: "domcontentloaded" });
  await expect(page).toHaveURL(/\/settings\/audit(?:\?|$)/);
  const auditReview = await fetchJson(page, "/api/audit/security-events?page=1&pageSize=25&sortBy=1&descending=true");
  expect(auditReview.status).toBe(200);
  expect(auditReview.body).toMatchObject({
    items: expect.any(Array),
    capacityThresholdReached: expect.any(Boolean),
  });
  expect((auditReview.body as { items: unknown[] }).items.length).toBeGreaterThan(0);
  await expect(page.getByRole("heading", { level: 1 })).toBeVisible({ timeout: 30_000 });
  await expect(page.getByRole("columnheader").first()).toBeVisible();
  await expect(page.locator("tbody tr").first()).toBeVisible();

  const crossTenantAudit = await generatedAuthenticationRequest(
    request,
    () => auditSecurityEventsList(
      { page: 1, pageSize: 25, sortBy: 1, descending: true },
      { headers: { Cookie: cookieHeader, [TENANT_HOST_HEADER]: "tenant-b.localhost" } },
    ),
    cookieHeader,
    "tenant-b.localhost",
  );
  expect(crossTenantAudit.status).toBe(401);

  expect(mockServiceWorkerRequests).toEqual([]);
  const [patientCount, reservations, shifts, reminders] = await Promise.all([
    postJson(page, "/api/modules/clinic/patients/count", {}),
    postJson(page, "/api/modules/appointments/reservations", { descriptor: {} }),
    postJson(page, "/api/modules/appointments/shifts", { descriptor: {} }),
    postJson(page, "/api/modules/communications/reminders/query", {}),
  ]);
  expect(patientCount.status).toBe(200);
  expect(patientCount.body).toBe(1);
  expect(reservations.status).toBe(200);
  expect(reservations.body).toEqual(expect.arrayContaining([expect.objectContaining({ status: 1 })]));
  expect(shifts.status).toBe(200);
  expect(shifts.body).toEqual(expect.arrayContaining([expect.objectContaining({ totalSeats: 2 })]));
  expect(reminders.status).toBe(200);
  expect(reminders.body).toEqual(expect.arrayContaining([expect.objectContaining({ isSent: false, subject: "E2E reminder" })]));
  expect(apiRequests.some((request) => request.url.endsWith("/api/modules/authentication/session"))).toBe(true);
  expect(apiRequests.some((request) => request.url.endsWith("/api/modules/appointments/health"))).toBe(true);
  // Sign-in and clinic requests each carry their own host across the handoff.
  expect(apiRequests.filter((request) => request.method !== "OPTIONS").every((request) => request.tenantHost === new URL(request.url).host)).toBe(true);

  const logout = await generatedAuthenticationRequest(
    request,
    () => authenticationLogoutPost(
      { logoutId: null, returnUrl: null },
      { headers: { Cookie: cookieHeader, [TENANT_HOST_HEADER]: tenantHost } },
    ),
    cookieHeader,
    tenantHost,
  );
  expect(logout.status).toBe(200);
  expect(logout.body).toMatchObject({ signedOut: true });
  const afterLogout = await generatedAuthenticationRequest(
    request,
    () => authenticationSessionGet({ headers: { Cookie: cookieHeader, [TENANT_HOST_HEADER]: tenantHost } }),
    cookieHeader,
    tenantHost,
  );
  expect(afterLogout.status).toBe(401);
});

test("requires reauthentication after a security attribute change", async ({ page }) => {
  await login(page, e2eUsername, "", e2ePassword, "198.51.100.10");

  const temporaryPassword = "E2E-Temporary-Password-774!";
  const rejectedPasswordChange = await postJson(page, "/api/modules/authentication/account/password", {
    currentPassword: "E2E-Wrong-Current-Password",
    newPassword: temporaryPassword,
  });
  expect(rejectedPasswordChange.status).toBe(401);
  expect(JSON.stringify(rejectedPasswordChange.body)).not.toContain(temporaryPassword);

  let changed = false;
  try {
    const updatedPassword = await postJson(page, "/api/modules/authentication/account/password", {
      currentPassword: e2ePassword,
      newPassword: temporaryPassword,
    });
    expect(updatedPassword.status, JSON.stringify(updatedPassword.body)).toBe(200);
    const sessionAfterPasswordChange = await fetchJson(page, "/api/modules/authentication/session");
    expect(sessionAfterPasswordChange.status).toBe(200);
    changed = true;
  } finally {
    if (changed) {
      const restoredPassword = await postJson(page, "/api/modules/authentication/account/password", {
        currentPassword: temporaryPassword,
        newPassword: e2ePassword,
      });
      expect(restoredPassword.status).toBe(200);
    }
  }
});

test("rejects an authenticated browser mutation without antiforgery proof", async ({ page, request }) => {
  await login(page, e2eUsername, "", e2ePassword, "198.51.100.11");

  const response = await page.evaluate(async ({ origin }) => {
    const result = await fetch(`${origin}/api/modules/clinic/patients/count`, {
      method: "POST",
      credentials: "include",
      headers: {
        "Content-Type": "application/json",
        "X-Vitarish-Tenant-Host": window.location.host,
      },
      body: JSON.stringify({}),
    });
    return { status: result.status, body: await result.json() as { code?: string } };
  }, { origin: browserApiOrigin });

  expect(response.status).toBe(400);
  expect(response.body.code).toBe(ANTIFORGERY_VALIDATION_FAILED_CODE);

  const administratorContext = await page.context().browser()!.newContext();
  const administratorPage = await administratorContext.newPage();
  try {
    await login(administratorPage, platformAdminUsername, platformAdminOrigin, e2ePassword, '198.51.100.12');
    const rejectedAuthenticationPolicyWrite = await administratorPage.evaluate(async ({ origin }) => {
      const currentPolicyResponse = await fetch(`${origin}/api/modules/authentication/security/authentication-policy`, {
        credentials: 'include',
        headers: {
          'X-Vitarish-Tenant-Host': window.location.host,
        },
      });
      if (!currentPolicyResponse.ok) {
        throw new Error(`Authentication policy read failed with status ${currentPolicyResponse.status}.`);
      }
      const currentPolicy = await currentPolicyResponse.json() as {
        minimumPasswordLength: number;
        requirePasswordLowercase: boolean;
        requirePasswordUppercase: boolean;
        requirePasswordNumber: boolean;
        requirePasswordSpecialCharacter: boolean;
        failedAttemptThreshold: number;
        failedAttemptDenialDurationSeconds: number;
        failedAttemptResponse: string;
        enabledAuthenticationMechanisms: string[];
        permittedPreAuthenticationActions: string[];
        version: number;
      };
      const result = await fetch(`${origin}/api/modules/authentication/security/authentication-policy`, {
        method: 'PUT',
        credentials: 'include',
        headers: {
          'Content-Type': 'application/json',
          'X-Vitarish-Tenant-Host': window.location.host,
        },
        body: JSON.stringify({
          minimumPasswordLength: currentPolicy.minimumPasswordLength,
          requirePasswordLowercase: currentPolicy.requirePasswordLowercase,
          requirePasswordUppercase: currentPolicy.requirePasswordUppercase,
          requirePasswordNumber: currentPolicy.requirePasswordNumber,
          requirePasswordSpecialCharacter: currentPolicy.requirePasswordSpecialCharacter,
          failedAttemptThreshold: currentPolicy.failedAttemptThreshold,
          failedAttemptDenialDurationSeconds: currentPolicy.failedAttemptDenialDurationSeconds,
          failedAttemptResponse: currentPolicy.failedAttemptResponse,
          enabledAuthenticationMechanisms: currentPolicy.enabledAuthenticationMechanisms,
          permittedPreAuthenticationActions: currentPolicy.permittedPreAuthenticationActions,
          expectedVersion: currentPolicy.version,
        }),
      });
      return { status: result.status, body: await result.json() as { code?: string } };
    }, { origin: browserApiOrigin });
    expect(rejectedAuthenticationPolicyWrite.status).toBe(400);
    expect(rejectedAuthenticationPolicyWrite.body.code).toBe(ANTIFORGERY_VALIDATION_FAILED_CODE);
  } finally {
    await administratorContext.close();
  }


  const cookieHeader = (await page.context().cookies())
    .map(cookie => `${cookie.name}=${cookie.value}`)
    .join("; ");
  const hostileOrigin = await request.post(`${apiOrigin}/api/modules/authentication/account/password`, {
    data: {
      currentPassword: "E2E-Wrong-Current-Password",
      newPassword: "E2E-Hostile-Origin-Password-774!",
    },
    headers: {
      Cookie: cookieHeader,
      Origin: "https://evil.example",
      Referer: "https://evil.example/account",
      "X-Vitarish-Tenant-Host": "localhost",
      "X-Request-Id": "e2e-hostile-origin-csrf",
    },
    ignoreHTTPSErrors: true,
  });
  expect(hostileOrigin.status()).toBe(401);
});

test("rejects direct anonymous calls to protected APIs", async ({ request }) => {
  const response = await request.get(`${apiOrigin}/api/modules/appointments/health`, {
    headers: { "X-Vitarish-Tenant-Host": "app.localhost" },
    ignoreHTTPSErrors: true,
  });

  expect(response.status()).toBe(401);

  const auditResponse = await request.get(`${apiOrigin}/api/audit/security-events?page=1&pageSize=25&sortBy=1&descending=true`, {
    headers: { "X-Vitarish-Tenant-Host": "localhost" },
    ignoreHTTPSErrors: true,
  });
  expect(auditResponse.status()).toBe(401);
});

test("rejects invalid password authentication without creating a session", async ({ request }) => {
  const response = await request.post(`${apiOrigin}/api/modules/authentication/login/password`, {
    data: {
      userNameOrEmail: "unknown-e2e-user@example.test",
      password: "wrong-password-not-a-secret",
      returnUrl: null,
    },
    headers: { "X-Vitarish-Tenant-Host": "localhost" },
    ignoreHTTPSErrors: true,
  });
  expect(response.status()).toBe(401);
  const errorBody = await response.text();
  expect(errorBody).not.toContain("wrong-password-not-a-secret");

  const session = await request.get(`${apiOrigin}/api/modules/authentication/session`, {
    headers: { "X-Vitarish-Tenant-Host": "localhost" },
    ignoreHTTPSErrors: true,
  });
  expect(session.status()).toBe(401);
});

test("normalizes unsafe authentication return URLs at the real API boundary", async ({ request }) => {
  expect(e2ePassword).toBeTruthy();

  const unsafeReturnUrls = [
    "https://evil.example/steal",
    "//evil.example/steal",
    "javascript:alert(1)",
    "\\\\evil.example\\steal",
  ];

  for (const [index, returnUrl] of unsafeReturnUrls.entries()) {
    const response = await request.post(`${apiOrigin}/api/modules/authentication/login/password`, {
      data: {
        userNameOrEmail: e2eUsername,
        password: e2ePassword,
        returnUrl,
      },
      headers: {
        "X-Vitarish-Tenant-Host": "localhost",
        [forwardedForHeaderName]: `198.51.100.${120 + index}`,
      },
      ignoreHTTPSErrors: true,
    });

    expect(response.status(), returnUrl).toBe(200);
    await expect(response.json(), returnUrl).resolves.toMatchObject({ returnUrl: "/home" });
  }
});

test("rejects invalid OTP and passkey completions at the real API boundary", async ({ request }) => {
  const emailCompletion = await request.post(`${apiOrigin}/api/modules/authentication/login/email/complete`, {
    data: {
      challengeId: "missing-e2e-email-challenge",
      code: "000000",
      returnUrl: null,
    },
    headers: {
      "X-Vitarish-Tenant-Host": "localhost",
      "X-Request-Id": "e2e-missing-email-challenge",
    },
    ignoreHTTPSErrors: true,
  });
  expect(emailCompletion.status()).toBe(401);

  const passkeyCompletion = await request.post(`${apiOrigin}/api/modules/authentication/login/passkey/complete`, {
    data: {
      challengeId: "",
      credentialJson: "",
      returnUrl: null,
    },
    headers: {
      "X-Vitarish-Tenant-Host": "localhost",
      "X-Request-Id": "e2e-empty-passkey-completion",
    },
    ignoreHTTPSErrors: true,
  });
  expect(passkeyCompletion.status()).toBe(400);
});

test("completes a real email OTP once and rejects its replay", async ({ request }) => {
  expect(smtpCapturePath).toBeTruthy();

  const begin = await request.post(`${apiOrigin}/api/modules/authentication/login/email/begin`, {
    data: { email: "e2e-owner@example.test", returnUrl: "/home" },
    headers: {
      "X-Vitarish-Tenant-Host": "localhost",
      [forwardedForHeaderName]: "198.51.100.141",
      "X-Request-Id": "e2e-email-otp-begin",
    },
    ignoreHTTPSErrors: true,
  });
  expect(begin.status()).toBe(200);
  const challenge = await begin.json() as { challengeId?: string };
  expect(challenge.challengeId).toEqual(expect.any(String));

  await expect.poll(() => existsSync(smtpCapturePath!)).toBe(true);
  const capturedMessage = readFileSync(smtpCapturePath!, "utf8");
  const code = /Your verification code is:\s*(?:<strong>)?([^<\s]+)(?:<\/strong>)?/i.exec(capturedMessage)?.[1];
  expect(code).toEqual(expect.any(String));

  const completed = await request.post(`${apiOrigin}/api/modules/authentication/login/email/complete`, {
    data: { challengeId: challenge.challengeId, code, returnUrl: "/home" },
    headers: {
      "X-Vitarish-Tenant-Host": "localhost",
      [forwardedForHeaderName]: "198.51.100.141",
      "X-Request-Id": "e2e-email-otp-complete",
    },
    ignoreHTTPSErrors: true,
  });
  expect(completed.status()).toBe(200);
  await expect(completed.json()).resolves.toMatchObject({ returnUrl: "/home" });

  const session = await request.get(`${apiOrigin}/api/modules/authentication/session`, {
    headers: { "X-Vitarish-Tenant-Host": "localhost" },
    ignoreHTTPSErrors: true,
  });
  expect(session.status()).toBe(200);
  await expect(session.json()).resolves.toMatchObject({ isAuthenticated: true });

  const replay = await request.post(`${apiOrigin}/api/modules/authentication/login/email/complete`, {
    data: { challengeId: challenge.challengeId, code, returnUrl: "/home" },
    headers: {
      "X-Vitarish-Tenant-Host": "localhost",
      [forwardedForHeaderName]: "198.51.100.141",
      "X-Request-Id": "e2e-email-otp-replay",
    },
    ignoreHTTPSErrors: true,
  });
  expect(replay.status()).toBe(400);
  await expect(replay.json()).resolves.toMatchObject({ code: "challenge_already_used" });
});

test("registers a passkey through the real browser ceremony once", async ({ page }) => {
  const cdp = await page.context().newCDPSession(page);
  await cdp.send("WebAuthn.enable");
  const { authenticatorId } = await cdp.send("WebAuthn.addVirtualAuthenticator", {
    options: {
      protocol: "ctap2",
      ctap2Version: "ctap2_1",
      transport: "internal",
      hasResidentKey: true,
      hasUserVerification: true,
      automaticPresenceSimulation: true,
      isUserVerified: true,
    },
  });

  await login(page, e2eUsername, "", e2ePassword, "198.51.100.13");
  await page.goto("/account/passkeys", { waitUntil: "domcontentloaded" });
  await page.getByRole("button", { name: "Register New Passkey" }).click();
  await expect(page.getByText("Passkey registered successfully")).toBeVisible();

  const credentials = await cdp.send("WebAuthn.getCredentials", { authenticatorId });
  expect(credentials.credentials).toHaveLength(1);
});

test("rejects malformed and invalid bearer tokens at the real API boundary", async ({ request }) => {
  const now = Math.floor(Date.now() / 1000);
  const cases = [
    { name: "malformed", token: "not-a-jwt" },
    {
      name: "expired",
      token: unsignedBearerToken({ iss: apiOrigin, aud: "vitarish-api", typ: "at+jwt", exp: now - 60 }),
    },
    {
      name: "wrong issuer",
      token: unsignedBearerToken({ iss: "https://wrong-issuer.invalid", aud: "vitarish-api", typ: "at+jwt", exp: now + 300 }),
    },
    {
      name: "wrong audience",
      token: unsignedBearerToken({ iss: apiOrigin, aud: "wrong-audience", typ: "at+jwt", exp: now + 300 }),
    },
    {
      name: "wrong token type",
      token: unsignedBearerToken({ iss: apiOrigin, aud: "vitarish-api", typ: "JWT", exp: now + 300 }),
    },
  ];

  for (const testCase of cases) {
    const response = await request.get(`${apiOrigin}/api/modules/appointments/health`, {
      headers: {
        Authorization: `Bearer ${testCase.token}`,
        "X-Vitarish-Tenant-Host": "app.localhost",
      },
      ignoreHTTPSErrors: true,
    });
    expect(response.status(), testCase.name).toBe(401);
  }
});

// authorization-matrix:intentional-anonymous
test("allows intentional anonymous operations to reach their owning boundary", async ({ request }) => {
  const response = await request.get(`${apiOrigin}/api/modules/authentication/antiforgery`, {
    headers: { "X-Vitarish-Tenant-Host": "app.localhost" },
    ignoreHTTPSErrors: true,
  });

  expect(response.status()).toBe(200);
  await expect(response.json()).resolves.toMatchObject({
    headerName: "X-XSRF-TOKEN",
    requestToken: expect.any(String),
  });

  const contact = await request.post(`${apiOrigin}/api/modules/communications/contact-inquiries`, {
    data: {},
    headers: { "X-Vitarish-Tenant-Host": "app.localhost" },
    ignoreHTTPSErrors: true,
  });
  expect(contact.status()).toBe(400);

  const emailLoginBegin = await request.post(`${apiOrigin}/api/modules/authentication/login/email/begin`, {
    data: {},
    headers: {
      "X-Vitarish-Tenant-Host": "localhost",
      "X-Request-Id": "authorization-matrix-email-login",
    },
    ignoreHTTPSErrors: true,
  });
  expect(emailLoginBegin.status()).toBe(400);

  const challenge = "authorization-matrix-proof";
  const social = await request.get(`${apiOrigin}/webhooks/social/instagram?hub.challenge=${challenge}`, {
    headers: { "X-Vitarish-Tenant-Host": "app.localhost" },
    ignoreHTTPSErrors: true,
  });
  expect(social.status()).toBe(200);
  expect(await social.text()).toBe(challenge);

  const oversizedWebhook = await request.post(`${apiOrigin}/webhooks/social/instagram`, {
    data: Buffer.alloc(1024 * 1024 + 1, "x"),
    headers: {
      "Content-Type": "text/plain",
      "X-Vitarish-Tenant-Host": "app.localhost",
      "X-Request-Id": "authorization-matrix-oversized-webhook",
    },
    ignoreHTTPSErrors: true,
  });
  expect(oversizedWebhook.status()).toBe(413);
});

// authorization-matrix:main-module-boundary
test("enforces tenant-user and tenant-owner authorization through the API", async ({ page }) => {
  await login(page, tenantUserUsername, "", e2ePassword, "198.51.100.15");
  const session = await fetchJson(page, "/api/modules/authentication/session");
  expect(session.status).toBe(200);
  expect(session.body).toMatchObject({
    isAuthenticated: true,
    tenantAccess: { canAccessTenantOwner: false, canAccessTenantUser: true },
  });

  const tenantHealth = await fetchJson(page, "/api/modules/appointments/health");
  expect(tenantHealth.status).toBe(200);

  const ownerOnly = await postJson(page, "/api/modules/clinic/patients/find-or-create", {
    identificationCode: "E2E-0001",
  });
  expect(ownerOnly.status).toBe(403);
  const userAdministration = await fetchJson(page, "/api/modules/users/search?query=e2e");
  const otherUserProfile = await fetchJson(page, `/api/modules/users/${tenantBOwnerUserId}/profile`);
  expect(userAdministration.status).toBe(403);
  expect(otherUserProfile.status).toBe(403);
});

// authorization-matrix:tenant-business-profile
test("only the tenant owner reads and replaces the tenant business profile", async ({ browser }) => {
  const profilePath = "/api/modules/parties/tenant-profile";
  const profile = {
    description: "E2E business profile",
    category: "dermatology",
    businessDetails: {
      address: { province: "Tehran", city: "Tehran", street: null, postalCode: null, latitude: 35.7, longitude: 51.4 },
      phones: ["021 1234 5678"],
      website: "https://e2e.example.test",
      socialLinks: [],
      workingHours: [{ day: "Saturday", intervals: [{ opens: "09:00", closes: "13:00" }, { opens: "16:00", closes: "20:00" }] }],
      photoFileIds: [],
    },
  };

  const tenantUserPage = await browser.newPage();
  await login(tenantUserPage, tenantUserUsername, "", e2ePassword, "198.51.100.26");
  expect((await fetchJson(tenantUserPage, profilePath)).status).toBe(403);
  expect((await putJson(tenantUserPage, profilePath, profile)).status).toBe(403);
  expect((await putJson(tenantUserPage, `${profilePath}/logo`, { fileId: crypto.randomUUID() })).status).toBe(403);
  await tenantUserPage.close();

  const ownerPage = await browser.newPage();
  await login(ownerPage, e2eUsername, "", e2ePassword, "198.51.100.27");
  const original = await fetchJson(ownerPage, profilePath);
  expect(original.status).toBe(200);
  expect((await putJson(ownerPage, profilePath, { ...profile, category: "astrology" })).status).toBe(400);
  expect((await putJson(ownerPage, profilePath, {
    ...profile,
    businessDetails: { ...profile.businessDetails, photoFileIds: [crypto.randomUUID()] },
  })).status).toBe(400);
  expect((await putJson(ownerPage, `${profilePath}/logo`, { fileId: crypto.randomUUID() })).status).toBe(400);
  expect((await putJson(ownerPage, profilePath, profile)).status).toBe(204);
  const saved = await fetchJson(ownerPage, profilePath);
  expect(saved.status).toBe(200);
  expect(saved.body).toMatchObject({
    description: profile.description,
    category: profile.category,
    businessDetails: { phones: profile.businessDetails.phones, workingHours: profile.businessDetails.workingHours },
  });

  // The owner tenant is shared fixture state, so put back what it had.
  const { description, category, businessDetails } = original.body as Record<string, unknown>;
  expect((await putJson(ownerPage, profilePath, { description, category, businessDetails })).status).toBe(204);
  await ownerPage.close();
});

test("UsersSearchUsers requires the platform administrator policy", async ({ browser }) => {
  const tenantUserPage = await browser.newPage();
  await login(tenantUserPage, tenantUserUsername, "", e2ePassword, "198.51.100.25");
  const denied = await fetchJson(tenantUserPage, "/api/modules/users/search?query=e2e");
  expect(denied.status).toBe(403);
  await tenantUserPage.close();

  const administratorPage = await browser.newPage();
  await login(administratorPage, platformAdminUsername, platformAdminOrigin, e2ePassword, "198.51.100.26");
  const allowed = await fetchJson(administratorPage, "/api/modules/users/search?query=e2e");
  expect(allowed.status).toBe(200);
  await administratorPage.close();
});

test("UsersGetProfile denies a different authenticated subject", async ({ page }) => {
  await login(page, tenantUserUsername, "", e2ePassword, "198.51.100.27");
  const otherUserProfile = await fetchJson(page, `/api/modules/users/${tenantBOwnerUserId}/profile`);
  expect(otherUserProfile.status).toBe(403);
});

test("User-auth matrix denies out-of-scope operation access per operation", async ({ page }) => {
  await login(page, tenantUserUsername, "", e2ePassword, "198.51.100.28");

  const matrixPath = join(process.cwd(), "../../../docs/security/authorization-matrix.md");
  const matrixText = readFileSync(matrixPath, "utf8");
  const matrixRows = matrixText.split(/\r?\n/)
    .map(line => line.split("|").map(cell => cell.trim().replace(/^`|`$/g, "")))
    .filter(cells => cells.length === 10 && cells[0] === "" && cells[9] === "");

  const openApiDocuments = ["Main_users.json", "Main_authentication.json"].map(fileName =>
    JSON.parse(readFileSync(join(process.cwd(), "src/api/specs", fileName), "utf8")) as {
    paths: Record<string, Record<string, { operationId?: string }>>;
  });
  const operations = openApiDocuments.flatMap(document => Object.entries(document.paths).flatMap(([route, methods]) =>
    Object.entries(methods).flatMap(([method, definition]) => definition.operationId
      ? [{ operationId: definition.operationId, method: method.toUpperCase(), route }]
      : [])));

  for (const row of matrixRows) {
    const operationId = row[1];
    const selfScoped = row[4] === "Self only" && row[5] === "Self only" && row[6] === "Self only";
    const tenantUserDenied = row[6] === "Deny";
    if (!selfScoped && !tenantUserDenied) continue;
    const operation = operations.find(candidate => candidate.operationId === operationId);
    expect(operation, `${operationId} exists in a Users or Authentication OpenAPI contract`).toBeDefined();
    if (!operation) continue;

    const checksDifferentSubject = selfScoped && operation.route.includes("{userId}");
    const requestPath = operation.route.replace(/\{([^}]+)\}/g, (_match, parameter: string) => {
      if (parameter === "userId") return tenantBOwnerUserId;
      if (parameter === "tenantId") return tenantAId;
      if (parameter === "credentialId") return "00000000-0000-0000-0000-000000000000";
      if (parameter === "roleSuffix") return "TenantUser";
      if (parameter === "token") return "authorization-matrix-token";
      throw new Error(`No test value is defined for OpenAPI parameter ${parameter} in ${operationId}.`);
    });
    const body = operation.route.endsWith("/complete-register")
      ? { attestationResponseJson: "{}" }
      : operation.method === "PUT" && operation.route.endsWith("/profile")
        ? { firstName: null, lastName: null }
        : operation.method === "PUT" && operation.route.endsWith("/culture")
          ? { culture: "en" }
          : {};
    const response = operation.method === "GET"
      ? await fetchJson(page, requestPath)
      : operation.method === "POST"
        ? await postJson(page, requestPath, body)
        : operation.method === "PUT"
          ? await putJson(page, requestPath, body)
          : operation.method === "DELETE"
            ? await deleteJson(page, requestPath, body)
            : (() => { throw new Error(`Unsupported self-service HTTP method ${operation.method} for ${operationId}.`); })();
    if (tenantUserDenied || checksDifferentSubject)
      expect(response.status, `${operationId} denies a different subject or TenantUser`).toBe(403);
    else
      expect([200, 201, 202, 204, 400, 404, 409, 410, 422, 429], `${operationId} reaches its authenticated subject boundary`)
        .toContain(response.status);
  }
});

test("keeps two tenants isolated across identifiers, alternate routes, search, and host forwarding", async ({ page, request }) => {
  await login(page, e2eUsername, "", e2ePassword, "198.51.100.14");

  const substitutedDetails = await fetchJson(page, `/api/modules/clinic/patients/${tenantBPatientId}`);
  const substitutedUpdate = await putJson(page, `/api/modules/clinic/patients/${tenantBPatientId}`, {
    identificationCode: "E2E-FORGED-TENANT-B-UPDATE",
    firstName: "Forged",
    lastName: "Tenant",
    email: "forged-tenant-b-update@example.test",
    phone: "+989000000099",
  });
  expect(substitutedDetails.status).toBe(404);
  expect(substitutedUpdate.status).toBe(404);
  expect(JSON.stringify(substitutedDetails.body)).not.toContain("E2E Tenant B Patient");
  expect(JSON.stringify(substitutedUpdate.body)).not.toContain("E2E Tenant B Patient");

  const substitutedSearch = await postJson(page, "/api/modules/clinic/patients/search", {
    search: null,
    skip: 0,
    take: 100,
    template: false,
  });
  expect(substitutedSearch.status).toBe(200);
  expect(JSON.stringify(substitutedSearch.body)).not.toContain("E2E Tenant B Patient");

  const canonicalAssistant = await postJson(page, `/api/modules/clinic/patients/${tenantBPatientId}/assistant`, { message: "read patient" });
  const legacyAgent = await postJson(page, `/api/modules/clinic/patients/${tenantBPatientId}/agent`, { message: "read patient" });
  const intelligencePatientConversation = await postJson(page, "/api/modules/intelligence/agent-conversations", {
    agentKey: "clinic.assistant.patient",
    subjectKind: "patient",
    subjectId: tenantBPatientId,
    message: "read patient",
  });
  const intelligencePracticeConversation = await postJson(page, "/api/modules/intelligence/agent-conversations", {
    agentKey: "clinic.assistant.operations",
    subjectKind: "practice",
    subjectId: tenantBId,
    message: "list patients",
  });
  expect(canonicalAssistant.status).not.toBe(200);
  expect(legacyAgent.status).not.toBe(200);
  expect(intelligencePatientConversation.status).toBe(404);
  expect(intelligencePracticeConversation.status).toBe(404);
  expect(JSON.stringify(canonicalAssistant.body)).not.toContain("E2E Tenant B Patient");
  expect(JSON.stringify(legacyAgent.body)).not.toContain("E2E Tenant B Patient");
  expect(JSON.stringify(intelligencePatientConversation.body)).not.toContain("E2E Tenant B Patient");
  expect(JSON.stringify(intelligencePracticeConversation.body)).not.toContain("E2E Tenant B Patient");

  const substitutedFileInformation = await fetchJson(page, `/api/modules/files/${tenantBFileId}`);
  const substitutedFileContent = await fetchJson(page, `/api/modules/files/${tenantBFileId}/content`);
  const substitutedFileSearch = await postJson(page, "/api/modules/files/search", { descriptor: { skip: 0, take: 100 } });
  const tenantAFileContent = await fetchText(page, `/api/modules/files/${tenantAFileId}/content`);
  expect(tenantAFileContent.status).toBe(200);
  expect(tenantAFileContent.body).toBe("tenant-a-file");
  expect(substitutedFileInformation.status).toBe(404);
  expect(substitutedFileContent.status).toBe(404);
  expect(substitutedFileSearch.status).toBe(200);
  expect(JSON.stringify(substitutedFileSearch.body)).not.toContain("tenant-b/file.txt");
  expect(JSON.stringify(substitutedFileInformation.body)).not.toContain("tenant-b/file.txt");

  const substitutedTenantList = await fetchJson(page, "/api/modules/parties/tenants");
  const substitutedTenant = await fetchJson(page, `/api/modules/parties/tenants/${tenantBId}`);
  const substitutedAdministrationTenant = await fetchJson(page, `/api/modules/parties/administration/tenants/${tenantBId}`);
  const substitutedUser = await fetchJson(page, `/api/modules/users/${tenantBOwnerUserId}`);
  const substitutedProfile = await fetchJson(page, `/api/modules/users/${tenantBOwnerUserId}/profile`);
  const substitutedTenantMutation = await postJson(page, "/api/modules/parties/tenants", {
    id: tenantBId,
    ownerId: "00000000-0000-0000-0000-000000000001",
    name: "forged tenant mutation",
    appId: "forged-app",
  });
  const substitutedTenantDeletion = await deleteJson(page, `/api/modules/parties/tenants/${tenantBId}`, {});
  expect(substitutedTenantList.status).toBe(403);
  expect(substitutedTenant.status).toBe(403);
  expect(substitutedAdministrationTenant.status).toBe(403);
  expect(substitutedUser.status).toBe(403);
  expect(substitutedProfile.status).toBe(403);
  expect(substitutedTenantMutation.status).toBe(403);
  expect(substitutedTenantDeletion.status).toBe(403);
  expect(JSON.stringify(substitutedTenant.body)).not.toContain("E2E Clinic B");
  expect(JSON.stringify(substitutedUser.body)).not.toContain(tenantBOwnerUsername);

  const tenantACookieHeader = (await page.context().cookies()).map(cookie => `${cookie.name}=${cookie.value}`).join("; ");
  let generatedSubstitutedUserStatus = 0;
  try {
    await generatedUserGet(request, tenantBOwnerUserId, tenantACookieHeader, "localhost");
  } catch (error) {
    generatedSubstitutedUserStatus = (error as { status?: number }).status ?? -1;
  }
  expect(generatedSubstitutedUserStatus).toBe(403);

  const substitutedSearchTenant = await postJson(page, "/api/modules/search", {
    indexName: null,
    query: "Tenant B",
    options: { tenantId: tenantBId },
  });
  expect(substitutedSearchTenant.status).toBe(403);

  await page.goto(tenantBOrigin, { waitUntil: "domcontentloaded" });
  const forwardedTenant = await postJson(page, "/api/modules/clinic/patients/count", {});
  expect(forwardedTenant.status).toBe(401);

  const explicitForwardedHost = await page.evaluate(async ({ origin, patientId }) => {
    const response = await fetch(`${origin}/api/modules/clinic/patients/${patientId}`, {
      credentials: "include",
      headers: {
        "X-Vitarish-Tenant-Host": "tenant-b.localhost",
        "X-Forwarded-Host": "tenant-b.localhost",
      },
    });
    return { status: response.status };
  }, { origin: browserApiOrigin, patientId: tenantBPatientId });
  expect(explicitForwardedHost.status).toBe(401);

  const tenantBContext = await page.context().browser()!.newContext();
  const tenantBPage = await tenantBContext.newPage();
  await login(tenantBPage, tenantBOwnerUsername, tenantBOrigin, e2ePassword, "198.51.100.17");
  const tenantBSession = await fetchJson(tenantBPage, "/api/modules/authentication/session");
  expect(tenantBSession.body).toMatchObject({
    isAuthenticated: true,
    tenantAccess: { canAccessTenantOwner: true },
  });
  const tenantBDetails = await fetchJson(tenantBPage, `/api/modules/clinic/patients/${tenantBPatientId}`);
  expect(tenantBDetails.status).toBe(200);
  expect(tenantBDetails.body).toMatchObject({ id: tenantBPatientId, name: "E2E Tenant B Patient" });
  const tenantBProfile = await fetchJson(tenantBPage, `/api/modules/users/${tenantBOwnerUserId}/profile`);
  expect(tenantBProfile.status).toBe(200);
  expect(tenantBProfile.body).toMatchObject({ userId: tenantBOwnerUserId });
  const tenantBUserAdministration = await fetchJson(tenantBPage, "/api/modules/users/search?query=e2e");
  expect(tenantBUserAdministration.status).toBe(403);
  const tenantBCookieHeader = (await tenantBContext.cookies()).map(cookie => `${cookie.name}=${cookie.value}`).join("; ");
  const generatedTenantBUser = await generatedUserGet(request, tenantBOwnerUserId, tenantBCookieHeader, "tenant-b.localhost");
  expect(generatedTenantBUser).toMatchObject({ userId: tenantBOwnerUserId });
  const tenantBFileInformation = await fetchJson(tenantBPage, `/api/modules/files/${tenantBFileId}`);
  expect(tenantBFileInformation.status).toBe(200);
  expect(tenantBFileInformation.body).toMatchObject({ id: tenantBFileId, url: "e2e/tenant-b/file.txt" });
  const tenantBFileContent = await fetchText(tenantBPage, `/api/modules/files/${tenantBFileId}/content`);
  expect(tenantBFileContent.status).toBe(200);
  expect(tenantBFileContent.body).toBe("tenant-b-file");
  const tenantBFileSearch = await postJson(tenantBPage, "/api/modules/files/search", { descriptor: { skip: 0, take: 100 } });
  expect(tenantBFileSearch.status).toBe(200);
  expect((tenantBFileSearch.body as Array<{ id: string }>).map((file) => file.id).sort()).toEqual(
    [tenantBFileId, tenantBLogoFileId].sort(),
  );

  const tenantBList = await postJson(tenantBPage, "/api/modules/clinic/patients", { descriptor: {} });
  expect(tenantBList.status).toBe(200);
  expect(tenantBList.body).toEqual([expect.objectContaining({ id: tenantBPatientId })]);
  expect(JSON.stringify(tenantBList.body)).not.toContain(tenantAPatientId);
  const tenantBSearch = await postJson(tenantBPage, "/api/modules/clinic/patients/search", {
    search: "Tenant B",
    skip: 0,
    take: 100,
    template: false,
  });
  expect(tenantBSearch.status).toBe(200);
  expect(tenantBSearch.body).toEqual([expect.objectContaining({ id: tenantBPatientId })]);
  expect(JSON.stringify(tenantBSearch.body)).not.toContain(tenantAPatientId);

  const adminContext = await page.context().browser()!.newContext();
  const adminPage = await adminContext.newPage();
  let tenantBRoleRevoked = false;
  try {
    await login(adminPage, platformAdminUsername, platformAdminOrigin, e2ePassword, "198.51.100.18");
    const revoke = await deleteJson(
      adminPage,
      `/api/modules/users/administration/${encodeURIComponent(tenantBOwnerUserId)}/tenant-roles/${tenantBId}/TenantOwner`,
      { roleSuffix: "TenantOwner" },
    );
    tenantBRoleRevoked = revoke.status === 200;
    expect(revoke.status).toBe(409);
    expect(revoke.body).toMatchObject({
      succeeded: false,
      errors: [expect.objectContaining({ code: "ActiveSessionConflict" })],
    });

    const staleClaimDetails = await fetchJson(tenantBPage, `/api/modules/clinic/patients/${tenantBPatientId}`);
    expect(staleClaimDetails.status).toBe(200);
    const concurrentActiveReads = await Promise.all(
      Array.from({ length: 8 }, () => fetchJson(tenantBPage, `/api/modules/clinic/patients/${tenantBPatientId}`)),
    );
    expect(concurrentActiveReads.every((response) => response.status === 200)).toBe(true);
    const adminTenant = await fetchJson(adminPage, `/api/modules/parties/administration/tenants/${tenantBId}`);
    expect(adminTenant.status).toBe(200);
    expect(adminTenant.body).toMatchObject({ id: tenantBId, name: "E2E Clinic B" });
    await adminPage.goto(tenantBOrigin, { waitUntil: "domcontentloaded" });
    const adminTenantB = await fetchJson(adminPage, `/api/modules/clinic/patients/${tenantBPatientId}`);
    await adminPage.goto("http://localhost:3005", { waitUntil: "domcontentloaded" });
    const adminTenantAPage = await adminContext.newPage();
    await adminTenantAPage.goto("http://localhost:3005", { waitUntil: "domcontentloaded" });
    const adminTenantA = await fetchJson(adminTenantAPage, `/api/modules/clinic/patients/${tenantAPatientId}`);
    expect(adminTenantA.status).toBe(200);
    expect(adminTenantB.status).toBe(401);
    await adminTenantAPage.close();
  } finally {
    if (tenantBRoleRevoked) {
      const restore = await putJson(
        adminPage,
        `/api/modules/users/administration/${encodeURIComponent(tenantBOwnerUserId)}/tenant-roles/${tenantBId}/TenantOwner`,
        { roleSuffix: "TenantOwner" },
      );
      expect(restore.status).toBe(200);
      await tenantBPage.setExtraHTTPHeaders({ [forwardedForHeaderName]: "198.51.100.212" });
      try {
        await login(tenantBPage, tenantBOwnerUsername, tenantBOrigin, e2ePassword, "198.51.100.19");
      } finally {
        await tenantBPage.setExtraHTTPHeaders({});
      }
      const concurrentRestoredReads = await Promise.all(
        Array.from({ length: 8 }, () => fetchJson(tenantBPage, `/api/modules/clinic/patients/${tenantBPatientId}`)),
      );
      expect(concurrentRestoredReads.every((response) => response.status === 200)).toBe(true);
    }
    await adminPage.close();
    await adminContext.close();
    await tenantBPage.close();
    await tenantBContext.close();
  }
});

test("enforces platform-admin authorization through the API", async ({ page }) => {
  await login(page, platformAdminUsername, platformAdminOrigin, e2ePassword, "198.51.100.20");
  const session = await fetchJson(page, "/api/modules/authentication/session");
  expect(session.status).toBe(200);
  expect(session.body).toMatchObject({
    isAuthenticated: true,
    canAccessPlatformAdmin: true,
  });

  const platform = await fetchJson(page, "/api/modules/platform/locale-notifications");
  expect(platform.status).toBe(200);
  const managedUserSearch = await fetchJson(page, "/api/modules/users/search?query=e2e");
  const otherUserProfile = await fetchJson(page, `/api/modules/users/${tenantBOwnerUserId}/profile`);
  expect(managedUserSearch.status).toBe(200);
  expect(otherUserProfile.status).toBe(403);

});

test("shows a generic 403 page while recording each denied audit review once", async ({ page, request }, testInfo) => {
  test.setTimeout(120_000);
  await login(page, tenantBOwnerUsername, platformAdminOrigin, e2ePassword, "198.51.100.23");

  const administratorContext = await page.context().browser()!.newContext();
  const administratorPage = await administratorContext.newPage();
  try {
    await login(administratorPage, platformAdminUsername, platformAdminOrigin, e2ePassword, "198.51.100.24");
    // The configured CI retry uses another review operation to avoid sharing the
    // previous attempt's anonymous deduplication window.
    const expectedAnonymousOperation = testInfo.retry === 0
      ? "Audit_PlatformSecurityEvents_Export"
      : "Audit_PlatformSecurityEventCatalog_Get";
    const anonymousPath = testInfo.retry === 0
      ? getAuditPlatformSecurityEventsExportQueryKey()[0]
      : getAuditPlatformSecurityEventCatalogGetQueryKey()[0];
    // Only denied reviews are counted: the administrator's own list reads are recorded as succeeded views.
    const anonymousFilters: AuditPlatformSecurityEventsListParams = {
      page: 1,
      pageSize: 25,
      eventName: "security.audit.review",
      eventCategory: "audit-review",
      outcome: "denied",
      sortBy: auditSortFields.occurredAt,
      descending: true,
      uncataloguedOnly: false,
    };
    const administratorCookieHeader = (await administratorContext.cookies())
      .map(cookie => `${cookie.name}=${cookie.value}`)
      .join("; ");
    const platformAdminTenantHost = new URL(platformAdminOrigin).hostname;
    const readAnonymousReviewList = () => generatedAuthenticationRequest(
      request,
      () => auditPlatformSecurityEventsList(anonymousFilters),
      administratorCookieHeader,
      platformAdminTenantHost,
    );
    const anonymousBefore = await readAnonymousReviewList();
    expect(anonymousBefore.status).toBe(200);
    const anonymousBeforePage = anonymousBefore.body as SecurityAuditPageReadDto;
    const anonymousCountBefore = Number(anonymousBeforePage.totalCount);
    const anonymousRead = await request.get(`${apiOrigin}${anonymousPath}`, {
      headers: { [TENANT_HOST_HEADER]: platformAdminTenantHost },
      ignoreHTTPSErrors: true,
    });
    expect(anonymousRead.status()).toBe(401);
    const anonymousAfter = await readAnonymousReviewList();
    expect(anonymousAfter.status).toBe(200);
    const anonymousAfterPage = anonymousAfter.body as SecurityAuditPageReadDto;
    expect(Number(anonymousAfterPage.totalCount)).toBe(anonymousCountBefore + 1);
    expect(anonymousAfterPage.items).toEqual(expect.arrayContaining([
      expect.objectContaining({ eventName: "security.audit.review", eventCategory: "audit-review", outcome: "denied", actorKind: "anonymous", actorId: null, subjectId: null, operation: expectedAnonymousOperation }),
    ]));
    const deniedReviewPath = `/api/audit/platform/security-events?page=1&pageSize=25&eventName=security.audit.review&eventCategory=audit-review&operation=Audit_PlatformSecurityEvents_List&outcome=denied&actorId=${encodeURIComponent(tenantBOwnerUserId)}&sortBy=0&descending=true&uncataloguedOnly=false`;
    const reviewEventsBefore = await fetchJson(administratorPage, deniedReviewPath);
    expect(reviewEventsBefore.status).toBe(200);
    const reviewEventCountBefore = (reviewEventsBefore.body as { totalCount: number }).totalCount;

    await page.goto(`${platformAdminOrigin}/`, { waitUntil: "domcontentloaded" });
    await expect(page.getByRole("heading", { name: "Forbidden" })).toBeVisible({ timeout: 30_000 });
    await expect(page.getByText("403", { exact: true })).toBeVisible();

    if (forbiddenScreenshotPath) {
      mkdirSync(dirname(forbiddenScreenshotPath), { recursive: true });
      await page.screenshot({ path: forbiddenScreenshotPath, fullPage: true });
    }

    const deniedAuditRead = await fetchJson(page, "/api/audit/platform/security-events?page=1&pageSize=25&sortBy=1&descending=true&uncataloguedOnly=false");
    expect(deniedAuditRead.status).toBe(403);

    const reviewEventsAfter = await fetchJson(administratorPage, deniedReviewPath);
    expect(reviewEventsAfter.status).toBe(200);
    expect((reviewEventsAfter.body as { totalCount: number }).totalCount).toBe(reviewEventCountBefore + 1);

    const platformOriginLogin = await fetchJson(
      administratorPage,
      `/api/audit/platform/security-events?page=1&pageSize=25&eventCategory=authentication&eventName=authentication.password.login&operation=password.login&outcome=succeeded&subjectId=${encodeURIComponent(tenantBOwnerUserId)}&sortBy=1&descending=true&uncataloguedOnly=false`,
    );
    expect(platformOriginLogin.status).toBe(200);
    expect((platformOriginLogin.body as { items?: Array<{ eventName?: string; subjectId?: string; clientInterfaceHost?: string | null }> }).items).toEqual(expect.arrayContaining([
      expect.objectContaining({
        eventName: "authentication.password.login",
        subjectId: tenantBOwnerUserId,
        clientInterfaceHost: new URL(platformAdminOrigin).hostname,
      }),
    ]));
  } finally {
    await administratorPage.close();
    await administratorContext.close();
  }
});

test("audits denied user creation and editing requests", async ({ page }) => {
  test.setTimeout(120_000);
  await login(page, tenantBOwnerUsername, platformAdminOrigin, e2ePassword, "198.51.100.25");

  const deniedCreate = await postJson(page, "/api/modules/users/create/email", {
    email: `unauthorized-create-${Date.now()}@example.test`,
    firstName: "Denied",
    lastName: "Creation",
  });
  expect(deniedCreate.status).toBe(403);

  const deniedEdit = await putJson(page, `/api/modules/users/${tenantBOwnerUserId}`, {
    email: "denied-edit@example.test",
    phoneNumber: null,
    firstName: "Denied",
    lastName: "Edit",
  });
  expect(deniedEdit.status).toBe(403);

  const deniedAuthenticationPolicyRead = await fetchJson(page, "/api/modules/authentication/security/authentication-policy");
  expect(deniedAuthenticationPolicyRead.status).toBe(403);
  const deniedAuthenticationPolicyChange = await putJson(page, "/api/modules/authentication/security/authentication-policy", {});
  expect(deniedAuthenticationPolicyChange.status).toBe(403);

  const administratorContext = await page.context().browser()!.newContext();
  const administratorPage = await administratorContext.newPage();
  try {
    await login(administratorPage, platformAdminUsername, platformAdminOrigin, e2ePassword, "198.51.100.26");

    const creationAudit = await fetchJson(
      administratorPage,
      `/api/audit/platform/security-events?page=1&pageSize=25&eventName=users.account.creation-failed&eventCategory=security-failure&operation=users.account.create&outcome=denied&actorId=${encodeURIComponent(tenantBOwnerUserId)}&sortBy=0&descending=true&uncataloguedOnly=false`,
    );
    expect(creationAudit.status).toBe(200);
    expect((creationAudit.body as { items?: Array<{ eventName?: string; outcome?: string; redactedReason?: string }> }).items)
      .toEqual(expect.arrayContaining([
        expect.objectContaining({
          eventName: "users.account.creation-failed",
          outcome: "denied",
          redactedReason: "unauthorized",
        }),
      ]));

    const editAudit = await fetchJson(
      administratorPage,
      `/api/audit/platform/security-events?page=1&pageSize=25&eventName=users.account.edit-failed&eventCategory=security-failure&operation=users.account.edit&outcome=denied&actorId=${encodeURIComponent(tenantBOwnerUserId)}&subjectId=${encodeURIComponent(tenantBOwnerUserId)}&sortBy=0&descending=true&uncataloguedOnly=false`,
    );
    expect(editAudit.status).toBe(200);
    expect((editAudit.body as { items?: Array<{ eventName?: string; outcome?: string; redactedReason?: string; subjectId?: string }> }).items)
      .toEqual(expect.arrayContaining([
        expect.objectContaining({
          eventName: "users.account.edit-failed",
          outcome: "denied",
          redactedReason: "unauthorized",
          subjectId: tenantBOwnerUserId,
        }),
      ]));
  } finally {
    await administratorPage.close();
    await administratorContext.close();
  }
});

// platform-audit
test("proves the global audit grid across a separate platform-admin origin", async ({ page }) => {
  test.setTimeout(300_000);
  await login(page, platformAdminUsername, platformAdminOrigin, e2ePassword, twoHostE2ePlatformAdminClientIp);
  const tenantBContext = await page.context().browser()!.newContext();
  const tenantBPage = await tenantBContext.newPage();
  const ownerContext = await page.context().browser()!.newContext();
  const ownerPage = await ownerContext.newPage();

  try {
    await login(tenantBPage, tenantBOwnerUsername, tenantBOrigin, e2ePassword, "198.51.100.22");
    await page.goto(`${platformAdminOrigin}/audit`, { waitUntil: "domcontentloaded" });
    await expect(page).toHaveURL(/localhost:3006\/audit(?:\?|$)/);
    await expect(page.getByRole("heading", { name: "Global security audit" })).toBeVisible({ timeout: 30_000 });
    await expect(page.locator("tbody tr").first()).toBeVisible({ timeout: 30_000 });

    const tenants = await fetchJson(page, "/api/modules/parties/platform/tenants");
    expect(tenants.status).toBe(200);
    expect(tenants.body).toEqual(expect.arrayContaining([
      expect.objectContaining({ id: tenantAId }),
      expect.objectContaining({ id: tenantBId }),
    ]));

    const globalReview = await fetchJson(page, "/api/audit/platform/security-events?page=1&pageSize=100&sortBy=1&descending=true&uncataloguedOnly=false");
    expect(globalReview.status).toBe(200);
    const globalItems = (globalReview.body as { items: Array<{ id?: string; eventName?: string; tenantId?: string }> }).items;
    expect(globalItems.some(item => item.tenantId === tenantAId)).toBe(true);
    expect(globalItems.some(item => item.tenantId === tenantBId)).toBe(true);

    const selectedTenant = await fetchJson(page, `/api/audit/platform/security-events?page=1&pageSize=25&tenantId=${tenantAId}&sortBy=1&descending=true&uncataloguedOnly=false`);
    expect(selectedTenant.status).toBe(200);
    const selectedTenantItems = (selectedTenant.body as { items: Array<{ tenantId?: string }> }).items;
    expect(selectedTenantItems.length).toBeGreaterThan(0);
    expect(selectedTenantItems.every(item => item.tenantId === tenantAId)).toBe(true);

    const catalog = await fetchJson(page, "/api/audit/platform/security-event-catalog");
    expect(catalog.status).toBe(200);
    expect(catalog.body).toEqual(expect.arrayContaining([
      expect.objectContaining({ category: "authentication", eventName: "authentication.password.login", operation: "password.login", isWildcard: false }),
    ]));

    const exactCatalogFilter = await fetchJson(page, "/api/audit/platform/security-events?page=1&pageSize=25&eventCategory=authentication&eventName=authentication.password.login&operation=password.login&sortBy=1&descending=true&uncataloguedOnly=false");
    expect(exactCatalogFilter.status).toBe(200);
    const exactItems = (exactCatalogFilter.body as { items: Array<{ id?: string; actorId?: string; eventName?: string; eventCategory?: string; operation?: string }> }).items;
    expect(exactItems.length).toBeGreaterThan(0);
    expect(exactItems.every(item => item.eventName === "authentication.password.login" && item.eventCategory === "authentication" && item.operation === "password.login")).toBe(true);

    const exactEventId = exactItems[0].id;
    expect(exactEventId).toBeTruthy();
    const exactEventFilter = await fetchJson(page, `/api/audit/platform/security-events?page=1&pageSize=25&eventId=${exactEventId}&sortBy=1&descending=true&uncataloguedOnly=false`);
    expect(exactEventFilter.status).toBe(200);
    expect((exactEventFilter.body as { items: Array<{ id?: string }> }).items).toEqual([expect.objectContaining({ id: exactEventId })]);

    await page.getByRole("button", { name: "All categories" }).click();
    await page.getByRole("option", { name: "authentication" }).click();
    await page.getByRole("button", { name: "All event types" }).click();
    await page.getByRole("option", {
      name: localizePlatformAuditEvent("en", "authentication.password.login", "authentication.password.login"),
      exact: true,
    }).click();
    await page.getByRole("button", { name: "All operations" }).click();
    await page.getByRole("option", { name: "password.login", exact: true }).click();
    await page.getByRole("button", { name: "Apply filters" }).click();
    await expect(page.locator("tbody tr").first()).toBeVisible();
    const evidenceScreenshot = process.env.VITARISH_NMAIL_EVIDENCE_SCREENSHOT;
    if (evidenceScreenshot) {
      mkdirSync(dirname(evidenceScreenshot), { recursive: true });
      await page.screenshot({ path: evidenceScreenshot, fullPage: true });

      const organicEventNames = new Set(globalItems.map(item => item.eventName).filter((eventName): eventName is string => Boolean(eventName)));
      const organicCatalogEntries = (catalog.body as Array<{ category?: string; eventName?: string; operation?: string }>)
        .filter(entry => entry.category && entry.eventName && entry.operation && organicEventNames.has(entry.eventName));
      for (const entry of organicCatalogEntries) {
        await page.getByRole("button", { name: "Clear filters" }).click();
        await page.getByRole("button", { name: "All categories" }).click();
        await page.getByRole("option", {
          name: localizePlatformAuditCategory("en", entry.category, entry.category!),
          exact: true,
        }).click();
        await page.getByRole("button", { name: "All event types" }).click();
        await page.getByRole("option", {
          name: localizePlatformAuditEvent("en", entry.eventName, entry.eventName!),
          exact: true,
        }).click();
        await page.getByRole("button", { name: "All operations" }).click();
        await page.getByRole("option", { name: entry.operation!, exact: true }).click();
        await page.getByRole("button", { name: "Apply filters" }).click();
        if (await page.locator("tbody tr").count() > 0) {
          const safeEventName = entry.eventName!.replace(/[^a-z0-9.-]+/gi, "-");
          await page.screenshot({ path: join(dirname(evidenceScreenshot), `audit-${safeEventName}.png`), fullPage: true });
        }
      }
    }

    const [download] = await Promise.all([
      page.waitForEvent("download"),
      page.getByRole("button", { name: "Export CSV" }).click(),
    ]);
    const downloadPath = await download.path();
    expect(downloadPath).toBeTruthy();
    const csv = readFileSync(downloadPath!, "utf8");
    expect(csv).toContain("\"id\",\"tenantId\",\"eventCategory\"");
    expect(csv).not.toContain("idempotencyKey");
    expect(csv).not.toContain("payload");

    await page.locator("tbody tr").first().click();
    const detailDialog = page.getByRole("dialog");
    await expect(detailDialog).toBeInViewport();
    await expect(detailDialog.getByRole("heading", { name: "Event details" })).toBeVisible();
    await expect(detailDialog.getByText("This panel exposes review-safe metadata only.")).toBeVisible();
    const detailScreenshot = process.env[twoHostE2eProofScreenshotEnvironmentKey]?.replace(/\.png$/i, "-authenticated-boundary-audit-dialog.png");
    if (detailScreenshot) {
      mkdirSync(dirname(detailScreenshot), { recursive: true });
      expect(existsSync(detailScreenshot), `refusing to overwrite retained evidence at ${detailScreenshot}`).toBe(false);
      await page.screenshot({ path: detailScreenshot });
    }
    await detailDialog.getByRole("button", { name: "Close event details" }).click();

    await login(ownerPage, e2eUsername, webOrigin, e2ePassword, "198.51.100.23");
    await ownerPage.goto(`${platformAdminOrigin}/audit`, { waitUntil: "domcontentloaded" });
    const deniedTenants = await fetchJson(ownerPage, "/api/modules/parties/platform/tenants");
    expect(deniedTenants.status).toBe(403);
    expect(JSON.stringify(deniedTenants.body ?? "")).not.toContain(tenantAId);
    const denied = await fetchJson(ownerPage, "/api/audit/platform/security-events?page=1&pageSize=25&sortBy=1&descending=true&uncataloguedOnly=false");
    expect(denied.status).toBe(403);
    expect(JSON.stringify(denied.body ?? "")).not.toContain(tenantBId);

    await ownerPage.goto(`${webOrigin}${ClinicRoutes.Clinic.Patients.Index}`, { waitUntil: "domcontentloaded" });
    await expect(ownerPage.getByRole("toolbar", { name: "Patients", exact: true })).toBeVisible({ timeout: 30_000 });
    await expect(ownerPage.getByText("E2E Patient", { exact: true })).toBeVisible({ timeout: 30_000 });
    await expect(ownerPage.getByText("افزایش قد", { exact: true })).toBeVisible({ timeout: 30_000 });
    await expect(ownerPage.getByText("Patient tags", { exact: true })).toBeVisible();

    const proofScreenshot = process.env[twoHostE2eProofScreenshotEnvironmentKey]?.replace(/\.png$/i, "-authenticated-boundary-patient-tags.png");
    if (proofScreenshot) {
      mkdirSync(dirname(proofScreenshot), { recursive: true });
      expect(existsSync(proofScreenshot), `refusing to overwrite retained evidence at ${proofScreenshot}`).toBe(false);
      await ownerPage.screenshot({ path: proofScreenshot, fullPage: true });
    }
  } finally {
    await ownerPage.close();
    await ownerContext.close();
    await tenantBPage.close();
    await tenantBContext.close();
  }
});

test("requires the internal-service credential boundary before callback validation", async ({ request }) => {
  const callbackUrl = `${apiOrigin}/api/modules/intelligence/internal/agent/tools/execute`;
  const envelope = {
    version: 1,
    executionId: "e2e-internal-service-check",
    agentKey: "clinic.assistant.operations",
    tenantId: "9c8b6b3a-7d6e-4f75-a88d-7d3f5b1b0f92",
    practiceId: "9c8b6b3a-7d6e-4f75-a88d-7d3f5b1b0f92",
    subjectKind: "practice",
    subjectId: "9c8b6b3a-7d6e-4f75-a88d-7d3f5b1b0f92",
    authenticatedUserId: "7b0f8d4d-3d6c-4d43-9f5e-9d5d3f9e6f4c",
    conversationId: "e2e-conversation",
    toolCallId: "e2e-tool-call",
    toolName: "e2e-tool",
    credentialVersion: 1,
    expiresAt: new Date(Date.now() + 300_000).toISOString(),
    correlationId: "e2e-correlation",
    role: "TenantOwner",
    locale: "en-US",
    timeZone: "Asia/Tehran",
    calendar: "gregory",
    currentUtc: new Date().toISOString(),
    currentLocal: new Date().toISOString(),
    permissions: [],
  };
  const body = { executionEnvelope: envelope, argumentsJson: "{}" };

  const anonymous = await request.post(callbackUrl, {
    data: body,
    headers: { "X-Vitarish-Tenant-Host": "app.localhost" },
    ignoreHTTPSErrors: true,
  });
  expect(anonymous.status()).toBe(401);

  const malformedServiceCredential = await request.post(callbackUrl, {
    data: body,
    headers: {
      "X-Vitarish-Tenant-Host": "app.localhost",
      "X-Vitarish-Agent-Credential": "invalid",
    },
    ignoreHTTPSErrors: true,
  });
  expect(malformedServiceCredential.status()).toBe(401);
});

test("accepts one valid signed callback only within its real practice scope", async ({ request }) => {
  expect(agentSharedSecret).toBeTruthy();
  const expiresAt = new Date(Date.now() + 300_000);
  const envelope: Record<string, unknown> = {
    version: 1,
    executionId: "e2e-valid-callback-execution",
    agentKey: "clinic.assistant.operations",
    tenantId: tenantBId,
    practiceId: tenantBId,
    subjectKind: "practice",
    subjectId: tenantBId,
    authenticatedUserId: tenantBOwnerUserId,
    conversationId: "e2e-valid-callback-conversation",
    toolCallId: "e2e-valid-callback-tool-call",
    toolName: "list_operations_shifts",
    credentialVersion: 1,
    expiresAt: expiresAt.toISOString(),
    correlationId: "e2e-valid-callback-correlation",
    role: "TenantOwner",
    locale: "en-US",
    timeZone: "Asia/Tehran",
    calendar: "gregory",
    currentUtc: new Date().toISOString(),
    currentLocal: new Date().toISOString(),
    permissions: [],
  };
  const expirySeconds = Math.floor(expiresAt.getTime() / 1000);
  const credential = `${expirySeconds}.${base64UrlHmac(agentSharedSecret!, agentEnvelopeCanonical(envelope, false))}`;
  envelope.callbackSignature = base64UrlHmac(credential, agentEnvelopeCanonical(envelope, true));
  const body = { executionEnvelope: envelope, argumentsJson: "{}" };
  const headers = {
    "X-Vitarish-Tenant-Host": "localhost",
    "X-Vitarish-Agent-Credential": credential,
    "X-Request-Id": "e2e-valid-callback-request",
  };

  const scopeMismatch: Record<string, unknown> = {
    ...envelope,
    executionId: "e2e-cross-tenant-callback-execution",
    tenantId: "9c8b6b3a-7d6e-4f75-a88d-7d3f5b1b0f92",
    practiceId: "9c8b6b3a-7d6e-4f75-a88d-7d3f5b1b0f92",
    subjectId: "9c8b6b3a-7d6e-4f75-a88d-7d3f5b1b0f92",
  };
  const scopeCredential = `${expirySeconds}.${base64UrlHmac(agentSharedSecret!, agentEnvelopeCanonical(scopeMismatch, false))}`;
  scopeMismatch.callbackSignature = base64UrlHmac(scopeCredential, agentEnvelopeCanonical(scopeMismatch, true));
  const scopeMismatchResponse = await request.post(`${apiOrigin}/api/modules/intelligence/internal/agent/tools/execute`, {
    data: { executionEnvelope: scopeMismatch, argumentsJson: "{}" },
    headers: {
      "X-Vitarish-Tenant-Host": "localhost",
      "X-Vitarish-Agent-Credential": scopeCredential,
      "X-Request-Id": "e2e-cross-tenant-callback-request",
    },
    ignoreHTTPSErrors: true,
  });
  expect(scopeMismatchResponse.status()).toBe(403);

  const completed = await request.post(`${apiOrigin}/api/modules/intelligence/internal/agent/tools/execute`, {
    data: body, headers, ignoreHTTPSErrors: true,
  });
  const completedBody = await completed.json();
  expect(completed.status(), JSON.stringify(completedBody)).toBe(200);
  expect(completedBody).toMatchObject({ callId: "e2e-valid-callback-tool-call" });

  const replay = await request.post(`${apiOrigin}/api/modules/intelligence/internal/agent/tools/execute`, {
    data: body, headers: { ...headers, "X-Request-Id": "e2e-valid-callback-replay" }, ignoreHTTPSErrors: true,
  });
  expect(replay.status()).toBe(409);
});

test("honors the trusted ingress proto and retains HTTPS at the API boundary", async ({ request }) => {
  const forwardedHttp = await request.get(`${apiOrigin}/api/modules/appointments/health`, {
    headers: {
      [forwardedForHeaderName]: "127.0.0.1",
      "X-Forwarded-Host": "app.localhost",
      "X-Forwarded-Proto": "http",
    },
    ignoreHTTPSErrors: true,
    maxRedirects: 0,
  });

  expect([301, 307, 308]).toContain(forwardedHttp.status());
  expect(forwardedHttp.headers().location).toMatch(/^https:\/\//i);
});

test("allows only the configured Web origin at the real CORS boundary", async ({ request }) => {
  const allowedOrigin = "http://localhost:3005";
  const allowed = await request.fetch(`${apiOrigin}/api/modules/appointments/health`, {
    method: "OPTIONS",
    headers: {
      Origin: allowedOrigin,
      "Access-Control-Request-Method": "GET",
    },
    ignoreHTTPSErrors: true,
  });
  expect(allowed.status()).toBe(204);
  expect(allowed.headers()["access-control-allow-origin"]).toBe(allowedOrigin);

  const deniedOrigin = "https://evil.example";
  const denied = await request.fetch(`${apiOrigin}/api/modules/appointments/health`, {
    method: "OPTIONS",
    headers: {
      Origin: deniedOrigin,
      "Access-Control-Request-Method": "GET",
    },
    ignoreHTTPSErrors: true,
  });
  expect(denied.status()).toBe(204);
  expect(denied.headers()["access-control-allow-origin"]).toBeUndefined();
});

test("locks the disposable platform-admin account after repeated real password failures", async ({ request }) => {
  const statuses: number[] = [];
  const lockoutClientIp = "198.51.100.77";

  for (let attempt = 0; attempt < 6; attempt += 1) {
    const response = await request.post(`${apiOrigin}/api/modules/authentication/login/password`, {
      data: {
        userNameOrEmail: lockoutPlatformAdminUsername,
        password: "E2E-Lockout-Wrong-Password",
        returnUrl: null,
      },
      headers: {
        [forwardedForHeaderName]: lockoutClientIp,
        "X-Vitarish-Tenant-Host": "localhost",
        "X-Request-Id": `e2e-lockout-attempt-${attempt}`,
      },
      ignoreHTTPSErrors: true,
    });
    statuses.push(response.status());
  }

  expect(statuses.slice(0, 4).every(status => status === 401)).toBe(true);
  expect(statuses.slice(4).every(status => status === 423)).toBe(true);
});

test("rate-limits repeated passkey authentication attempts at the real API boundary", async ({ request }) => {
  const statuses: number[] = [];

  for (let attempt = 0; attempt < 11; attempt += 1) {
    const response = await request.post(`${apiOrigin}/api/modules/authentication/login/passkey/begin`, {
      data: { userNameOrEmail: null, returnUrl: null },
      headers: {
        "X-Vitarish-Tenant-Host": "localhost",
        "X-Request-Id": `e2e-passkey-rate-limit-${attempt}`,
      },
      ignoreHTTPSErrors: true,
    });
    statuses.push(response.status());
  }

  expect(statuses).toContain(429);
  expect(statuses.every((status) => status === 200 || status === 429)).toBe(true);
});

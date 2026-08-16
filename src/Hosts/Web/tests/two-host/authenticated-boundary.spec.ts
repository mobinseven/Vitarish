import { expect, test, type Page } from "@playwright/test";

const apiOrigin = process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://localhost:7272";
const e2eUsername = process.env.VITARISH_E2E_USERNAME ?? "e2e-owner";
const e2ePassword = process.env.VITARISH_E2E_PASSWORD;
const tenantUserUsername = process.env.VITARISH_E2E_TENANT_USER_USERNAME ?? "e2e-tenant-user";
const platformAdminUsername = process.env.VITARISH_E2E_PLATFORM_ADMIN_USERNAME ?? "e2e-platform-admin";

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
  }, { apiOrigin, path });
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
  }, { apiOrigin, path, body });
}

async function login(page: Page, username: string) {
  expect(e2ePassword).toBeTruthy();
  await page.goto("/login/password", { waitUntil: "domcontentloaded" });
  await page.locator('input[name="username"]').fill(username);
  await page.locator('input[name="password"]').fill(e2ePassword!);
  await Promise.all([
    page.waitForResponse((response) => response.url().endsWith("/api/modules/authentication/login/password") && response.status() === 200),
    page.locator('button[type="submit"]').click(),
  ]);
}

test("uses the real Web and Main hosts for an authenticated tenant-scoped request", async ({ page }) => {
  const apiRequests: Array<{ method: string; url: string; tenantHost: string | undefined }> = [];
  const mockServiceWorkerRequests: string[] = [];
  page.on("request", (request) => {
    if (request.url().startsWith(apiOrigin)) {
      apiRequests.push({ method: request.method(), url: request.url(), tenantHost: request.headers()["x-vitarish-tenant-host"] });
    }
    if (request.url().includes("mockServiceWorker.js")) mockServiceWorkerRequests.push(request.url());
  });
  await login(page, e2eUsername);
  await page.goto("/home", { waitUntil: "domcontentloaded" });

  await expect(page).toHaveURL(/\/home(?:\?|$)/);
  await expect(page.locator("body")).not.toContainText("MSW");

  const session = await fetchJson(page, "/api/modules/authentication/session");
  expect(session.status).toBe(200);
  expect(session.body).toMatchObject({
    isAuthenticated: true,
    tenantAccess: { canAccessTenantOwner: true },
  });

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

  expect(mockServiceWorkerRequests).toEqual([]);
  const [patientCount, reservations, shifts, reminders] = await Promise.all([
    postJson(page, "/api/modules/clinic/patients/count", {}),
    postJson(page, "/api/modules/appointments/reservations", { descriptor: {} }),
    postJson(page, "/api/modules/appointments/shifts", { descriptor: {} }),
    postJson(page, "/communications/reminders/query", {}),
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
  expect(apiRequests.filter((request) => request.method !== "OPTIONS").every((request) => request.tenantHost === new URL(page.url()).host)).toBe(true);

  const proofScreenshot = process.env.VITARISH_E2E_PROOF_SCREENSHOT;
  if (proofScreenshot) {
    await page.screenshot({ path: proofScreenshot, fullPage: true });
  }
});

test("rejects direct anonymous calls to protected APIs", async ({ request }) => {
  const response = await request.get(`${apiOrigin}/api/modules/appointments/health`, {
    headers: { "X-Vitarish-Tenant-Host": "app.localhost" },
    ignoreHTTPSErrors: true,
  });

  expect(response.status()).toBe(401);
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

  const contact = await request.post(`${apiOrigin}/communications/contact-inquiries`, {
    data: {},
    headers: { "X-Vitarish-Tenant-Host": "app.localhost" },
    ignoreHTTPSErrors: true,
  });
  expect(contact.status()).toBe(400);

  const emailLoginBegin = await request.post(`${apiOrigin}/api/modules/authentication/login/email/begin`, {
    data: {},
    headers: { "X-Vitarish-Tenant-Host": "localhost" },
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
});

// authorization-matrix:main-module-boundary
test("enforces tenant-user and tenant-owner authorization through the API", async ({ page }) => {
  await login(page, tenantUserUsername);
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
});

test("enforces platform-admin authorization through the API", async ({ page }) => {
  await login(page, platformAdminUsername);
  const session = await fetchJson(page, "/api/modules/authentication/session");
  expect(session.status).toBe(200);
  expect(session.body).toMatchObject({
    isAuthenticated: true,
    canAccessPlatformAdmin: true,
  });

  const platform = await fetchJson(page, "/api/modules/platform/locale-notifications");
  expect(platform.status).toBe(200);

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

test("honors the trusted ingress proto and retains HTTPS at the API boundary", async ({ request }) => {
  const forwardedHttp = await request.get(`${apiOrigin}/api/modules/appointments/health`, {
    headers: {
      "X-Forwarded-For": "127.0.0.1",
      "X-Forwarded-Host": "app.localhost",
      "X-Forwarded-Proto": "http",
    },
    ignoreHTTPSErrors: true,
    maxRedirects: 0,
  });

  expect([301, 307, 308]).toContain(forwardedHttp.status());
  expect(forwardedHttp.headers().location).toMatch(/^https:\/\//i);
});

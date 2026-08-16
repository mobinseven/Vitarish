import { expect, test, type APIRequestContext, type Page } from "@playwright/test";
import { usersGetProfile } from "../../shared/auth-web/src/api/generated/users/users";

const apiOrigin = process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://localhost:7272";
const e2eUsername = process.env.VITARISH_E2E_USERNAME ?? "e2e-owner";
const e2ePassword = process.env.VITARISH_E2E_PASSWORD;
const tenantUserUsername = process.env.VITARISH_E2E_TENANT_USER_USERNAME ?? "e2e-tenant-user";
const tenantBOwnerUsername = process.env.VITARISH_E2E_TENANT_B_OWNER_USERNAME ?? "e2e-tenant-b-owner";
const platformAdminUsername = process.env.VITARISH_E2E_PLATFORM_ADMIN_USERNAME ?? "e2e-platform-admin";
const tenantBOrigin = "http://tenant-b.localhost:3005";
const tenantAPatientId = "2d7a3eb9-1d47-4c63-9a45-19d7e6a0fb18";
const tenantBPatientId = "f4c8109a-6f22-4a31-9ab8-48d7ad0d7e2f";
const tenantBId = "b2d2e4a1-2fd8-4781-8d9f-6c96f2a5e7b4";
const tenantBOwnerUserId = "c65fcb80-8db3-4e2f-b3b9-1c4d7b1e0a62";
const tenantAFileId = "4b8d1e7a-9f24-4d65-a8b1-6c0e2f5d7a91";
const tenantBFileId = "9e2f6a41-7c35-4b8d-a0e6-1f5d9c2b7a84";

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
  }, { apiOrigin, path, body });
}

async function fetchText(page: Page, path: string) {
  return page.evaluate(async ({ apiOrigin: origin, path: requestPath }) => {
    const response = await fetch(`${origin}${requestPath}`, {
      credentials: "include",
      headers: { "X-Vitarish-Tenant-Host": window.location.host },
    });
    return { status: response.status, body: await response.text() };
  }, { apiOrigin, path });
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
    return { status: response.status };
  }, { apiOrigin, path, body });
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

async function login(page: Page, username: string, origin = "") {
  expect(e2ePassword).toBeTruthy();
  await page.goto(`${origin}/login/password`, { waitUntil: "domcontentloaded" });
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

test("keeps two tenants isolated across identifiers, alternate routes, search, and host forwarding", async ({ page, request }) => {
  await login(page, e2eUsername);

  const substitutedDetails = await fetchJson(page, `/api/modules/clinic/patients/${tenantBPatientId}`);
  expect(substitutedDetails.status).toBe(404);
  expect(JSON.stringify(substitutedDetails.body)).not.toContain("E2E Tenant B Patient");

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

  const tenantACookieHeader = (await page.context().cookies(apiOrigin)).map(cookie => `${cookie.name}=${cookie.value}`).join("; ");
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
  expect(forwardedTenant.status).toBe(403);

  const explicitForwardedHost = await page.evaluate(async ({ origin, patientId }) => {
    const response = await fetch(`${origin}/api/modules/clinic/patients/${patientId}`, {
      credentials: "include",
      headers: {
        "X-Vitarish-Tenant-Host": "tenant-b.localhost",
        "X-Forwarded-Host": "tenant-b.localhost",
      },
    });
    return { status: response.status };
  }, { origin: apiOrigin, patientId: tenantBPatientId });
  expect(explicitForwardedHost.status).toBe(403);

  const tenantBContext = await page.context().browser()!.newContext();
  const tenantBPage = await tenantBContext.newPage();
  await login(tenantBPage, tenantBOwnerUsername, tenantBOrigin);
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
  const tenantBCookieHeader = (await tenantBContext.cookies(apiOrigin)).map(cookie => `${cookie.name}=${cookie.value}`).join("; ");
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
  expect(tenantBFileSearch.body).toEqual([expect.objectContaining({ id: tenantBFileId })]);

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
  await login(adminPage, platformAdminUsername, tenantBOrigin);
  const revoke = await deleteJson(
    adminPage,
    `/api/modules/users/administration/${encodeURIComponent(tenantBOwnerUserId)}/tenant-roles/${tenantBId}/TenantOwner`,
    { roleSuffix: "TenantOwner" },
  );
  expect(revoke.status).toBe(200);

  const staleClaimDetails = await fetchJson(tenantBPage, `/api/modules/clinic/patients/${tenantBPatientId}`);
  expect(staleClaimDetails.status).toBe(403);
  const adminTenant = await fetchJson(adminPage, `/api/modules/parties/administration/tenants/${tenantBId}`);
  expect(adminTenant.status).toBe(200);
  expect(adminTenant.body).toMatchObject({ id: tenantBId, name: "E2E Clinic B" });
  const adminTenantB = await fetchJson(adminPage, `/api/modules/clinic/patients/${tenantBPatientId}`);
  await adminPage.goto("http://localhost:3005", { waitUntil: "domcontentloaded" });
  const adminTenantAPage = await adminContext.newPage();
  await adminTenantAPage.goto("http://localhost:3005", { waitUntil: "domcontentloaded" });
  const adminTenantA = await fetchJson(adminTenantAPage, `/api/modules/clinic/patients/${tenantAPatientId}`);
  expect(adminTenantA.status).toBe(200);
  expect(adminTenantB.status).toBe(200);
  await adminTenantAPage.close();
  await adminPage.close();
  await adminContext.close();
  await tenantBPage.close();
  await tenantBContext.close();
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

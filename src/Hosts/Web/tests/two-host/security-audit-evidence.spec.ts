import { expect, test, type APIRequestContext, type Page } from "@playwright/test";
import { existsSync, mkdirSync, readFileSync } from "node:fs";
import { join } from "node:path";
import { Routes as ClinicRoutes } from "../../apps/private-practice-owner/src/app/routes";
import { Routes as PlatformRoutes } from "../../apps/platform-admin/src/app/routes";
import {
  localizePlatformAuditCategory,
  localizePlatformAuditEvent,
} from "../../shared/security/src/localization";

const apiOrigin = process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://localhost:7272";
const platformOrigin = process.env.PLAYWRIGHT_PLATFORM_ADMIN_ORIGIN ?? "http://localhost:3006";
const ownerUsername = process.env.VITARISH_E2E_USERNAME ?? "e2e-owner";
const ownerOrigin = "";
const platformAdminUsername = process.env.VITARISH_E2E_PLATFORM_ADMIN_USERNAME ?? "e2e-platform-admin";
const ownerTenantId = "9c8b6b3a-7d6e-4f75-a88d-7d3f5b1b0f92";
const tenantUserRoleSuffix = "TenantUser";
const password = process.env.VITARISH_E2E_PASSWORD;
const evidenceDirectory = process.env.VITARISH_E2E_AUDIT_EVIDENCE_DIRECTORY
  ?? join(process.cwd(), "artifacts", "two-host-e2e", "security-audit-evidence");
const locale = process.env.VITARISH_E2E_LOCALE === "fa" ? "fa" : "en";
const uiText = {
  clinicUsers: locale === "fa" ? "کاربران کلینیک" : "Clinic Users",
  addStaff: locale === "fa" ? "افزودن کارمند" : "Add staff member",
  memberCreated: locale === "fa" ? "حساب اکنون عضو مستأجر است." : "The account is now a tenant member.",
  removeAccess: locale === "fa" ? "حذف دسترسی" : "Remove access",
  memberRemoved: locale === "fa" ? "دسترسی مستأجر حذف شد." : "Tenant access removed.",
  changePassword: locale === "fa" ? "تغییر رمز عبور" : "Change Password",
  auditTitle: locale === "fa" ? "ممیزی امنیتی سراسری" : "Global security audit",
  clearFilters: locale === "fa" ? "پاک کردن فیلترها" : "Clear filters",
  allCategories: locale === "fa" ? "همه دسته‌ها" : "All categories",
  allEvents: locale === "fa" ? "همه انواع رویداد" : "All event types",
  allOperations: locale === "fa" ? "همه عملیات" : "All operations",
  applyFilters: locale === "fa" ? "اعمال فیلتر" : "Apply filters",
  outcome: locale === "fa" ? "نتیجه" : "Outcome",
  export: locale === "fa" ? "خروجی CSV" : "Export CSV",
  next: locale === "fa" ? "بعدی" : "Next",
} as const;

const auditEvents = {
  passwordLogin: { controls: [1, 8, 9, 10], category: "authentication", name: "authentication.password.login", operation: "password.login" },
  identityPasswordCheck: { controls: [1, 8, 9], category: "authentication", name: "authentication.identity.password-check", operation: "identity.password.check" },
  passwordChange: { controls: [1, 8, 10, 12], category: "authentication", name: "authentication.account.password.change", operation: "account.password.change" },
  passwordPolicyRejected: { controls: [1, 8, 10, 12], category: "authentication", name: "authentication.account.password.change", operation: "account.password.change" },
  sessionEstablished: { controls: [1, 8, 9, 21, 22], category: "session", name: "authentication.session.established", operation: "session.establish" },
  tenantMemberProvisioning: { controls: [1, 11, 17, 18], category: "management", name: "users.tenant-member-provisioning", operation: "Users_ProvisionTenantMember" },
  tenantMemberRemoval: { controls: [1, 11, 17, 18], category: "management", name: "users.tenant-member-removal", operation: "Users_RemoveTenantMember" },
  auditReview: { controls: [1, 3, 17], category: "audit-review", name: "security.audit.review", operation: "audit.security-events.list" },
  auditExport: { controls: [1, 15], category: "import-export", name: "security.data.export", operation: "data.export" },
  accessHistory: { controls: [1, 3, 17], category: "session", name: "authentication.access-history", operation: "access-history" },
  sessionPolicy: { controls: [1, 4, 16, 17], category: "authentication", name: "authentication.security-access-policy", operation: "Authentication_SecurityAccessPolicy_Change" },
  securityFunctionFailure: { controls: [19], category: "security-failure", name: "security.failure", operation: "security.failure" },
  tenantRoleReplacement: { controls: [11, 17, 18], category: "management", name: "users.tenant-role-replacement", operation: "Users_ReplaceTenantRole" },
  dataImport: { controls: [14], category: "import-export", name: "security.data.import", operation: "data.import" },
} as const;

type AuditEventExpectation = (typeof auditEvents)[keyof typeof auditEvents] & { outcome: string };
type AuditPage = { items: Array<{ id?: string; eventName?: string; eventCategory?: string; operation?: string; outcome?: string; actorId?: string; actorKind?: string; tenantId?: string; subjectId?: string; resource?: string; redactedReason?: string }> };

async function login(page: Page, username: string, origin = "", clientIp = "198.51.100.60"): Promise<void> {
  await page.context().addCookies([
    { name: "vitarish.locale", value: locale, url: "http://localhost:3005/" },
    { name: "vitarish.locale", value: locale, url: platformOrigin },
  ]);
  expect(password, "VITARISH_E2E_PASSWORD must be supplied by the disposable E2E environment").toBeTruthy();
  await page.setExtraHTTPHeaders({ "X-Forwarded-For": clientIp });
  if (origin === platformOrigin) {
    await page.goto(`${platformOrigin}${PlatformRoutes.LoginPassword}`, { waitUntil: "networkidle" });
    await expect(page.locator('input[name="username"]')).toBeVisible({ timeout: 30_000 });
    await expect(page.locator('button[type="submit"]')).toBeEnabled({ timeout: 30_000 });
  } else {
    await page.goto(`${origin}${ClinicRoutes.Login.Password}`, { waitUntil: "domcontentloaded" });
  }
  await page.locator('input[name="username"]').fill(username);
  await page.locator('input[name="password"]').fill(password!);
  const [response] = await Promise.all([
    page.waitForResponse((candidate) => candidate.url().endsWith("/api/modules/authentication/login/password")),
    page.locator('button[type="submit"]').click(),
  ]);
  expect(response.status()).toBe(200);
}

async function platformAuditQuery(page: Page, event: AuditEventExpectation): Promise<AuditPage> {
  const result = await page.evaluate(async ({ origin, expected }) => {
    const url = new URL("/api/audit/platform/security-events", origin);
    url.search = new URLSearchParams({
      page: "1",
      pageSize: "25",
      eventCategory: expected.category,
      eventName: expected.name,
      operation: expected.operation,
      outcome: expected.outcome,
      sortBy: "1",
      descending: "true",
      uncataloguedOnly: "false",
    }).toString();
    const response = await fetch(url, { credentials: "include" });
    return { status: response.status, body: await response.json() };
  }, { origin: apiOrigin, expected: event });

  expect(result.status).toBe(200);
  return result.body as AuditPage;
}

async function postJson(page: Page, path: string, body: unknown = null): Promise<{ status: number; body: unknown }> {
  return page.evaluate(async ({ origin, requestPath, requestBody }) => {
    const antiforgeryResponse = await fetch(`${origin}/api/modules/authentication/antiforgery`, { credentials: "include" });
    const antiforgery = await antiforgeryResponse.json() as { headerName: string; requestToken: string };
    const response = await fetch(`${origin}${requestPath}`, {
      method: "POST",
      credentials: "include",
      headers: {
        "Content-Type": "application/json",
        "X-Vitarish-Tenant-Host": window.location.host,
        "X-Request-Id": `e2e-audit-${crypto.randomUUID()}`,
        [antiforgery.headerName]: antiforgery.requestToken,
      },
      body: JSON.stringify(requestBody),
    });
    return { status: response.status, body: await response.json().catch(() => null) };
  }, { origin: apiOrigin, requestPath: path, requestBody: body });
}

async function putJson(page: Page, path: string, body: unknown): Promise<{ status: number; body: unknown }> {
  return page.evaluate(async ({ origin, requestPath, requestBody }) => {
    const antiforgeryResponse = await fetch(`${origin}/api/modules/authentication/antiforgery`, { credentials: "include" });
    const antiforgery = await antiforgeryResponse.json() as { headerName: string; requestToken: string };
    const response = await fetch(`${origin}${requestPath}`, {
      method: "PUT",
      credentials: "include",
      headers: {
        "Content-Type": "application/json",
        "X-Vitarish-Tenant-Host": window.location.host,
        "X-Request-Id": `e2e-audit-${crypto.randomUUID()}`,
        [antiforgery.headerName]: antiforgery.requestToken,
      },
      body: JSON.stringify(requestBody),
    });
    return { status: response.status, body: await response.json().catch(() => null) };
  }, { origin: apiOrigin, requestPath: path, requestBody: body });
}

async function deleteJson(page: Page, path: string, body: unknown = null): Promise<{ status: number; body: unknown }> {
  return page.evaluate(async ({ origin, requestPath, requestBody }) => {
    const antiforgeryResponse = await fetch(`${origin}/api/modules/authentication/antiforgery`, { credentials: "include" });
    const antiforgery = await antiforgeryResponse.json() as { headerName: string; requestToken: string };
    const response = await fetch(`${origin}${requestPath}`, {
      method: "DELETE",
      credentials: "include",
      headers: {
        "Content-Type": "application/json",
        "X-Vitarish-Tenant-Host": window.location.host,
        "X-Request-Id": `e2e-audit-${crypto.randomUUID()}`,
        [antiforgery.headerName]: antiforgery.requestToken,
      },
      body: JSON.stringify(requestBody),
    });
    return { status: response.status, body: await response.json().catch(() => null) };
  }, { origin: apiOrigin, requestPath: path, requestBody: body });
}

async function uploadEvidenceFile(page: Page): Promise<{ status: number; body: unknown }> {
  return page.evaluate(async ({ origin }) => {
    const antiforgeryResponse = await fetch(`${origin}/api/modules/authentication/antiforgery`, { credentials: "include" });
    const antiforgery = await antiforgeryResponse.json() as { headerName: string; requestToken: string };
    const content = new TextEncoder().encode(`e2e-audit-import-${crypto.randomUUID()}`);
    const digest = await crypto.subtle.digest("SHA-256", content);
    const checksum = Array.from(new Uint8Array(digest), value => value.toString(16).padStart(2, "0")).join("");
    const form = new FormData();
    form.append("content", new Blob([content], { type: "text/plain" }), "audit-evidence.txt");
    form.append("fileName", "audit-evidence.txt");
    form.append("contentType", "text/plain");
    form.append("size", String(content.byteLength));
    form.append("checksum", checksum);
    const response = await fetch(`${origin}/api/modules/files/upload`, {
      method: "POST",
      credentials: "include",
      headers: {
        "X-Vitarish-Tenant-Host": window.location.host,
        "X-Request-Id": `e2e-audit-import-${crypto.randomUUID()}`,
        [antiforgery.headerName]: antiforgery.requestToken,
      },
      body: form,
    });
    return { status: response.status, body: await response.json().catch(() => null) };
  }, { origin: apiOrigin });
}

async function captureAuditEvidence(page: Page, event: AuditEventExpectation, filename: string): Promise<void> {
  const visibleCategory = localizePlatformAuditCategory(locale, event.category, event.category);
  const visibleEvent = localizePlatformAuditEvent(locale, event.name, event.name);
  const records = await platformAuditQuery(page, event);
  expect(records.items.length, `controls ${event.controls.join(", ")} / ${event.name} must have a durable event after its real action`).toBeGreaterThan(0);
  expect(records.items.every((item) =>
    item.eventName === event.name
    && item.eventCategory === event.category
    && item.operation === event.operation
    && item.outcome === event.outcome,
  )).toBe(true);
  expect(records.items.every((item) =>
    ["user", "anonymous", "system"].includes(item.actorKind ?? "")
    && (item.actorKind === "anonymous" || Boolean(item.actorId))
    && Boolean(item.tenantId)
    && Boolean(item.resource),
  )).toBe(true);
  await page.goto(`${platformOrigin}${PlatformRoutes.Audit}`, { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { name: uiText.auditTitle })).toBeVisible({ timeout: 30_000 });
  await page.getByRole("button", { name: uiText.clearFilters }).click();
  await page.getByRole("button", { name: uiText.allCategories }).click();
  await page.getByRole("option", { name: visibleCategory, exact: true }).click();
  await page.getByRole("button", { name: uiText.allEvents }).click();
  await page.getByRole("option", { name: visibleEvent, exact: true }).click();
  await page.getByRole("button", { name: uiText.allOperations }).click();
  await page.getByRole("option", { name: event.operation, exact: true }).click();
  await page.getByLabel(uiText.outcome).fill(event.outcome);
  const response = page.waitForResponse((candidate) =>
    candidate.url().includes("/api/audit/platform/security-events") && candidate.request().method() === "GET",
  );
  await page.getByRole("button", { name: uiText.applyFilters }).click();
  await response;
  const firstRow = page.locator("tbody tr").first();
  await expect(firstRow).toBeVisible({ timeout: 30_000 });
  await expect(firstRow.getByText(visibleEvent, { exact: true }).first()).toBeVisible();
  await expect(firstRow.getByText(event.operation, { exact: true }).last()).toBeVisible();
  await expect(firstRow.getByText(event.outcome, { exact: true })).toBeVisible();

  mkdirSync(evidenceDirectory, { recursive: true });
  await page.screenshot({ path: join(evidenceDirectory, filename), fullPage: true });
}

async function platformAuditCategoryQuery(page: Page, category: string, pageNumber: number): Promise<AuditPage & { page: number; pageSize: number; totalCount: number }> {
  const result = await page.evaluate(async ({ origin, eventCategory, page }) => {
    const url = new URL("/api/audit/platform/security-events", origin);
    url.search = new URLSearchParams({
      page: String(page),
      pageSize: "25",
      eventCategory,
      sortBy: "1",
      descending: "true",
      uncataloguedOnly: "false",
    }).toString();
    const response = await fetch(url, { credentials: "include" });
    return { status: response.status, body: await response.json() };
  }, { origin: apiOrigin, eventCategory: category, page: pageNumber });

  expect(result.status).toBe(200);
  return result.body as AuditPage & { page: number; pageSize: number; totalCount: number };
}

async function captureAllAuthenticationCatalogEvidence(page: Page): Promise<void> {
  const category = auditEvents.passwordLogin.category;
  const firstPage = await platformAuditCategoryQuery(page, category, 1);
  expect(firstPage.totalCount, "the authentication catalog must contain durable records").toBeGreaterThan(0);
  expect(firstPage.items.every((item) =>
    item.eventCategory === category
    && Boolean(item.eventName)
    && Boolean(item.operation),
  )).toBe(true);

  await page.goto(`${platformOrigin}${PlatformRoutes.Audit}`, { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { name: uiText.auditTitle })).toBeVisible({ timeout: 30_000 });
  await page.getByRole("button", { name: uiText.clearFilters }).click();
  await page.getByRole("button", { name: uiText.allCategories }).click();
  await page.getByRole("option", {
    name: localizePlatformAuditCategory(locale, category, category),
    exact: true,
  }).click();

  const applyResponse = page.waitForResponse((candidate) =>
    candidate.url().includes("/api/audit/platform/security-events") && candidate.request().method() === "GET",
  );
  await page.getByRole("button", { name: uiText.applyFilters }).click();
  await applyResponse;

  expect(firstPage.items.length).toBeGreaterThan(0);
  expect(firstPage.items.every((item) => item.eventCategory === category)).toBe(true);
  await expect(page.locator("tbody tr").first()).toBeVisible({ timeout: 30_000 });
  mkdirSync(evidenceDirectory, { recursive: true });
  await page.screenshot({
    path: join(evidenceDirectory, "authentication-catalog-page-1.png"),
    fullPage: true,
  });
}

test("real actions create visible platform security-audit evidence", async ({ page, browser, request }) => {
  test.setTimeout(300_000);
  const anonymousContext = await browser.newContext();
  const anonymousPage = await anonymousContext.newPage();
  const administratorContext = await browser.newContext();
  const administratorPage = await administratorContext.newPage();
  const temporaryPassword = `E2E-Audit-${Date.now()}-Temporary!`;

  try {
    await login(page, ownerUsername, ownerOrigin, "198.51.100.61");

    await anonymousPage.goto(ClinicRoutes.Login.Password, { waitUntil: "domcontentloaded" });
    await anonymousPage.locator('input[name="username"]').fill(ownerUsername);
    await anonymousPage.locator('input[name="password"]').fill("incorrect-e2e-audit-password");
    const failedLogin = anonymousPage.waitForResponse((candidate) => candidate.url().endsWith("/api/modules/authentication/login/password"));
    await anonymousPage.locator('button[type="submit"]').click();
    expect([400, 401]).toContain((await failedLogin).status());

    await page.goto(ClinicRoutes.Account.Password, { waitUntil: "domcontentloaded" });
    await page.locator('input[name="currentPassword"]').fill(password!);
    await page.locator('input[name="newPassword"]').fill("Ab1!xy");
    await page.locator('input[name="confirmPassword"]').fill("Ab1!xy");
    const rejectedPassword = page.waitForResponse((candidate) => candidate.url().endsWith("/api/modules/authentication/account/password"));
    await page.getByRole("button", { name: uiText.changePassword }).click();
    expect((await rejectedPassword).status()).toBe(400);

    await page.locator('input[name="currentPassword"]').fill(password!);
    await page.locator('input[name="newPassword"]').fill(temporaryPassword);
    await page.locator('input[name="confirmPassword"]').fill(temporaryPassword);
    const passwordChanged = page.waitForResponse((candidate) => candidate.url().endsWith("/api/modules/authentication/account/password"));
    await page.getByRole("button", { name: uiText.changePassword }).click();
    expect((await passwordChanged).status()).toBe(200);

    await page.locator('input[name="currentPassword"]').fill(temporaryPassword);
    await page.locator('input[name="newPassword"]').fill(password!);
    await page.locator('input[name="confirmPassword"]').fill(password!);
    const passwordRestored = page.waitForResponse((candidate) => candidate.url().endsWith("/api/modules/authentication/account/password"));
    await page.getByRole("button", { name: uiText.changePassword }).click();
    expect((await passwordRestored).status()).toBe(200);

    await page.goto(ClinicRoutes.Clinic.Settings.Users, { waitUntil: "domcontentloaded" });
    await expect(page.getByRole("heading", { name: uiText.clinicUsers, exact: true })).toBeVisible({ timeout: 30_000 });
    const contact = `e2e-audit-member-${Date.now()}@example.test`;
    await page.getByRole("textbox", { name: locale === "fa" ? "نشانی ایمیل" : "Email address" }).fill(contact);
    await page.getByRole("button", { name: uiText.addStaff }).click();
    await expect(page.getByText(uiText.memberCreated, { exact: true })).toBeVisible();
    const member = page.getByRole("listitem").filter({ hasText: contact });
    await expect(member).toBeVisible();

    await login(administratorPage, platformAdminUsername, platformOrigin, "198.51.100.62");
    const tenantMembers = await page.evaluate(async (origin) => {
      const response = await fetch(`${origin}/api/modules/users/tenant-members?maxResults=100`, {
        credentials: "include",
        headers: { "X-Vitarish-Tenant-Host": window.location.host },
      });
      return { status: response.status, body: await response.json() as Array<{ user?: { id?: string; email?: string }; id?: string; email?: string }> };
    }, apiOrigin);
    expect(tenantMembers.status).toBe(200);
    const memberId = tenantMembers.body.find(item => item.user?.email === contact || item.email === contact)?.user?.id ?? tenantMembers.body.find(item => item.user?.email === contact || item.email === contact)?.id;
    expect(memberId, "the newly provisioned member must expose its user identifier").toEqual(expect.any(String));
    const rolePath = `/api/modules/users/administration/${encodeURIComponent(memberId!)}/tenant-roles/${ownerTenantId}/${tenantUserRoleSuffix}`;
    expect((await deleteJson(administratorPage, rolePath, { roleSuffix: tenantUserRoleSuffix })).status).toBe(200);
    expect((await putJson(administratorPage, rolePath, { roleSuffix: tenantUserRoleSuffix })).status).toBe(200);

    page.once("dialog", (dialog) => dialog.accept());
    await member.getByRole("button", { name: uiText.removeAccess }).click();
    await expect(page.getByText(uiText.memberRemoved, { exact: true })).toBeVisible();

    const uploaded = await uploadEvidenceFile(page);
    expect(uploaded.status).toBe(200);

    const accessHistory = await page.evaluate(async (origin) => {
      const response = await fetch(`${origin}/api/modules/authentication/access-history`, { credentials: "include" });
      return { status: response.status, body: await response.json().catch(() => null) };
    }, apiOrigin);
    expect(accessHistory.status).toBe(200);
    expect((await postJson(page, "/api/modules/authentication/access-history/acknowledge")).status).toBe(200);

    // OTP establishes a second session and may invalidate the owner session under
    // the evaluated concurrent-session policy, so perform it after owner actions.
    await completeEmailOtp(request);

    const currentPolicy = await administratorPage.evaluate(async (origin) => {
      const response = await fetch(`${origin}/api/modules/authentication/security/session-policy`, { credentials: "include" });
      return { status: response.status, body: await response.json() as { maximumConcurrentSessions: number; concurrentSessionsFunctionallyRequired: boolean; defaultInactivityTimeoutMinutes: number; allowedSessionStartCidrs: string[] | null; version: number } };
    }, apiOrigin);
    expect(currentPolicy.status).toBe(200);
    const policy = currentPolicy.body;
    const rejectedPolicyUpdate = await putJson(administratorPage, "/api/modules/authentication/security/session-policy", {
      maximumConcurrentSessions: policy.maximumConcurrentSessions,
      concurrentSessionsFunctionallyRequired: policy.concurrentSessionsFunctionallyRequired,
      defaultInactivityTimeoutMinutes: 31,
      allowedSessionStartCidrs: policy.allowedSessionStartCidrs,
      expectedVersion: policy.version,
    });
    expect(rejectedPolicyUpdate.status).toBe(400);

    const policyUpdate = await putJson(administratorPage, "/api/modules/authentication/security/session-policy", {
      maximumConcurrentSessions: 2,
      concurrentSessionsFunctionallyRequired: true,
      defaultInactivityTimeoutMinutes: policy.defaultInactivityTimeoutMinutes,
      allowedSessionStartCidrs: policy.allowedSessionStartCidrs,
      expectedVersion: policy.version,
    });
    expect(policyUpdate.status).toBe(200);

    const secondAdministratorContext = await browser.newContext();
    const secondAdministratorPage = await secondAdministratorContext.newPage();
    const thirdAdministratorContext = await browser.newContext();
    const thirdAdministratorPage = await thirdAdministratorContext.newPage();
    try {
      await secondAdministratorPage.goto(`${platformOrigin}${PlatformRoutes.LoginPassword}`, { waitUntil: "networkidle" });
      await expect(secondAdministratorPage.locator('input[name="username"]')).toBeVisible({ timeout: 30_000 });
      await secondAdministratorPage.locator('input[name="username"]').fill(platformAdminUsername);
      await secondAdministratorPage.locator('input[name="password"]').fill(password!);
      const secondSession = secondAdministratorPage.waitForResponse((candidate) => candidate.url().endsWith("/api/modules/authentication/login/password"));
      await secondAdministratorPage.locator('button[type="submit"]').click();
      expect((await secondSession).status()).toBe(200);

      await thirdAdministratorPage.goto(`${platformOrigin}${PlatformRoutes.LoginPassword}`, { waitUntil: "networkidle" });
      await expect(thirdAdministratorPage.locator('input[name="username"]')).toBeVisible({ timeout: 30_000 });
      await thirdAdministratorPage.locator('input[name="username"]').fill(platformAdminUsername);
      await thirdAdministratorPage.locator('input[name="password"]').fill(password!);
      const deniedSession = thirdAdministratorPage.waitForResponse((candidate) => candidate.url().endsWith("/api/modules/authentication/login/password"));
      await thirdAdministratorPage.locator('button[type="submit"]').click();
      expect([400, 401, 403, 423, 503]).toContain((await deniedSession).status());
    } finally {
      await thirdAdministratorPage.close();
      await thirdAdministratorContext.close();
      await secondAdministratorPage.close();
      await secondAdministratorContext.close();
    }
    await captureAuditEvidence(administratorPage, { ...auditEvents.passwordLogin, outcome: "succeeded" }, "password-login-succeeded.png");
    await captureAuditEvidence(administratorPage, { ...auditEvents.passwordLogin, outcome: "denied" }, "password-login-denied.png");
    await captureAuditEvidence(administratorPage, { ...auditEvents.identityPasswordCheck, outcome: "denied" }, "identity-password-check-denied.png");
    await captureAuditEvidence(administratorPage, { ...auditEvents.sessionEstablished, outcome: "succeeded" }, "session-established.png");
    await captureAuditEvidence(administratorPage, { ...auditEvents.passwordChange, outcome: "succeeded" }, "password-change.png");
    const passwordPolicyRecords = await platformAuditQuery(administratorPage, { ...auditEvents.passwordPolicyRejected, outcome: "denied" });
    expect(passwordPolicyRecords.items.every((item) => item.redactedReason === "password_policy_rejected")).toBe(true);
    await captureAuditEvidence(administratorPage, { ...auditEvents.passwordPolicyRejected, outcome: "denied" }, "password-policy-rejected.png");
    await captureAuditEvidence(administratorPage, { ...auditEvents.tenantMemberProvisioning, outcome: "succeeded" }, "tenant-member-provisioned.png");
    await captureAuditEvidence(administratorPage, { ...auditEvents.tenantMemberRemoval, outcome: "succeeded" }, "tenant-member-removed.png");
    await captureAuditEvidence(administratorPage, { ...auditEvents.tenantRoleReplacement, outcome: "succeeded" }, "tenant-role-replaced.png");
    await captureAuditEvidence(administratorPage, { ...auditEvents.dataImport, outcome: "succeeded" }, "data-import.png");
    await captureAuditEvidence(administratorPage, { ...auditEvents.auditReview, outcome: "succeeded" }, "audit-review.png");
    await captureAuditEvidence(administratorPage, { ...auditEvents.accessHistory, outcome: "succeeded" }, "access-history.png");
    await captureAuditEvidence(administratorPage, { ...auditEvents.sessionPolicy, outcome: "succeeded" }, "session-policy.png");
    const securityFailureRecords = await platformAuditQuery(administratorPage, { ...auditEvents.securityFunctionFailure, outcome: "failed" });
    expect(securityFailureRecords.items.every((item) => item.redactedReason === "validation_failed")).toBe(true);
    await captureAuditEvidence(administratorPage, { ...auditEvents.securityFunctionFailure, outcome: "failed" }, "security-function-failure.png");
    await captureAuditEvidence(administratorPage, { ...auditEvents.sessionEstablished, outcome: "denied" }, "session-concurrent-limit-denied.png");

    const [download] = await Promise.all([
      administratorPage.waitForEvent("download"),
      administratorPage.getByRole("button", { name: uiText.export }).click(),
    ]);
    expect(await download.path()).toBeTruthy();
    await captureAuditEvidence(administratorPage, { ...auditEvents.auditExport, outcome: "succeeded" }, "audit-export.png");
    await captureAllAuthenticationCatalogEvidence(administratorPage);
  } finally {
    await administratorPage.close();
    await administratorContext.close();
    await anonymousPage.close();
    await anonymousContext.close();
  }
});

async function completeEmailOtp(request: APIRequestContext): Promise<void> {
  const smtpCapturePath = process.env.VITARISH_E2E_SMTP_CAPTURE_PATH;
  expect(smtpCapturePath, "VITARISH_E2E_SMTP_CAPTURE_PATH must be supplied for OTP evidence").toBeTruthy();
  const begin = await request.post(`${apiOrigin}/api/modules/authentication/login/email/begin`, {
    data: { email: "e2e-owner@example.test", returnUrl: "/home" },
    headers: { "X-Vitarish-Tenant-Host": "localhost", "X-Forwarded-For": "198.51.100.63", "X-Request-Id": `e2e-audit-otp-${Date.now()}` },
    ignoreHTTPSErrors: true,
  });
  expect(begin.status()).toBe(200);
  const challenge = await begin.json() as { challengeId?: string };
  expect(challenge.challengeId).toEqual(expect.any(String));
  await expect.poll(() => existsSync(smtpCapturePath!)).toBe(true);
  const capturedMessage = readFileSync(smtpCapturePath!, "utf8");
  const code = /Your verification code is:\s*(?:<strong>)?([^<\s]+)(?:<\/strong>)?/i.exec(capturedMessage)?.[1];
  expect(code).toEqual(expect.any(String));
  const complete = await request.post(`${apiOrigin}/api/modules/authentication/login/email/complete`, {
    data: { challengeId: challenge.challengeId, code, returnUrl: "/home" },
    headers: { "X-Vitarish-Tenant-Host": "localhost", "X-Forwarded-For": "198.51.100.63", "X-Request-Id": `e2e-audit-otp-complete-${Date.now()}` },
    ignoreHTTPSErrors: true,
  });
  expect(complete.status()).toBe(200);
}

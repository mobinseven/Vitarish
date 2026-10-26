import { expect, request as playwrightRequest, test, type APIRequestContext, type Page } from "@playwright/test";
import { createHmac } from "node:crypto";
import { existsSync, mkdirSync, readFileSync } from "node:fs";
import { join } from "node:path";
import { twoHostE2eApiOriginEnvironmentKey, twoHostE2eAuditEvidenceDirectoryEnvironmentKey, twoHostE2eFileTransferAuditKeyEnvironmentKey, twoHostE2eLocaleEnvironmentKey } from "../../scripts/two-host-e2e-artifacts.mjs";
import { TENANT_HOST_HEADER } from "../../shared/api-client/src/apiFetch";
import { Routes as ClinicRoutes } from "../../apps/private-practice-owner/src/app/routes";
import { Routes as PlatformRoutes } from "../../apps/platform-admin/src/app/routes";
import {
  localizePlatformAuditCategory,
  localizePlatformAuditChangeField,
  localizePlatformAuditEvent,
  localizeSecurityAuditOutcome,
  securityMessageCatalogs,
} from "../../shared/security/src/localization";

// Main itself, for API-boundary checks made without the browser.
const apiOrigin = process.env[twoHostE2eApiOriginEnvironmentKey] ?? "https://localhost:7272";
// In-page requests use the web origin's same-origin /api, as in production.
const browserApiOrigin = "";
const platformOrigin = process.env.PLAYWRIGHT_PLATFORM_ADMIN_ORIGIN ?? "http://localhost:3006";
const ownerUsername = process.env.VITARISH_E2E_USERNAME ?? "e2e-owner";
const ownerClientIp = "198.51.100.61";
const ownerOrigin = "";
const platformAdminUsername = process.env.VITARISH_E2E_PLATFORM_ADMIN_USERNAME ?? "e2e-platform-admin";
const ownerTenantId = "9c8b6b3a-7d6e-4f75-a88d-7d3f5b1b0f92";
const tenantUserRoleSuffix = "TenantUser";
const password = process.env.VITARISH_E2E_PASSWORD;
const evidenceDirectory = process.env[twoHostE2eAuditEvidenceDirectoryEnvironmentKey]
  ?? join(process.cwd(), "artifacts", "two-host-e2e", "security-audit-evidence");
const locale = process.env[twoHostE2eLocaleEnvironmentKey] === "fa" ? "fa" : "en";
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
  sourceThreshold: locale === "fa" ? "آستانه تلاش ناموفق احراز هویت برای هر نشانی IP" : "Failed authentication threshold per IP address",
  eventDetails: locale === "fa" ? "جزئیات رویداد" : "Event details",
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
  auditStorageThreshold: { controls: [5], category: "security-failure", name: "security.audit.storage-threshold", operation: "audit.storage.threshold" },
  integrityVerification: { controls: [7], category: "import-export", name: "security.data.integrity-verification", operation: "data.integrity.verify" },
  tenantRoleReplacement: { controls: [11, 17, 18], category: "management", name: "users.tenant-role-replacement", operation: "Users_ReplaceTenantRole" },
  dataImport: { controls: [14], category: "import-export", name: "security.data.import", operation: "data.import" },
  sourceBlocked: { controls: [9], category: "authentication", name: "authentication.source.blocked", operation: "authentication.source.block" },
} as const;

// E2E-host-only triggers mapped by src/Hosts/E2e; they run the real Files
// verification service and corrupt the E2E in-memory stored object.
const e2eAuditEvidenceBase = "/api/e2e/audit-evidence";
const storageThresholdCheckPath = `${e2eAuditEvidenceBase}/storage-threshold/check`;
const storeOutageStartPath = `${e2eAuditEvidenceBase}/store-outage/start`;
const storeOutageEndPath = `${e2eAuditEvidenceBase}/store-outage/end`;
const integrityVerificationPath = (fileId: string) => `${e2eAuditEvidenceBase}/files/${fileId}/integrity-verification`;
const integrityTamperPath = (fileId: string) => `${e2eAuditEvidenceBase}/files/${fileId}/tamper`;

type AuditEventExpectation = (typeof auditEvents)[keyof typeof auditEvents] & { outcome: string };
type AuditPage = { items: Array<{ id?: string; eventName?: string; eventCategory?: string; operation?: string; outcome?: string; actorId?: string; actorKind?: string; tenantId?: string; subjectId?: string; resource?: string; resourceNameHash?: string; resourceExtension?: string; resourceSize?: number; redactedReason?: string; sourceIpAddress?: string; changes?: Array<{ field: string; previousValue: string | null; newValue: string | null }> }>; totalCount: number };

function escapeRegExp(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}
const authenticationPolicyPath = "/api/modules/authentication/security/authentication-policy";
const passwordLoginPath = "/api/modules/authentication/login/password";

type AuthenticationPolicy = {
  minimumPasswordLength: number;
  requirePasswordLowercase: boolean;
  requirePasswordUppercase: boolean;
  requirePasswordNumber: boolean;
  requirePasswordSpecialCharacter: boolean;
  failedAttemptThreshold: number;
  sourceFailedAttemptThreshold: number;
  failedAttemptDenialDurationSeconds: number;
  failedAttemptResponse: string;
  enabledAuthenticationMechanisms: string[];
  permittedPreAuthenticationActions: string[];
  version: number;
};

async function readAuthenticationPolicy(page: Page): Promise<AuthenticationPolicy> {
  const result = await page.evaluate(async ({ origin, path }) => {
    const response = await fetch(`${origin}${path}`, { credentials: "include", headers: { "X-Vitarish-Tenant-Host": window.location.host } });
    return { status: response.status, body: await response.json() };
  }, { origin: browserApiOrigin, path: authenticationPolicyPath });
  expect(result.status).toBe(200);
  return result.body as AuthenticationPolicy;
}

async function saveSourceFailedAttemptThreshold(page: Page, threshold: number): Promise<void> {
  const policy = await readAuthenticationPolicy(page);
  const saved = await putJson(page, authenticationPolicyPath, {
    minimumPasswordLength: policy.minimumPasswordLength,
    requirePasswordLowercase: policy.requirePasswordLowercase,
    requirePasswordUppercase: policy.requirePasswordUppercase,
    requirePasswordNumber: policy.requirePasswordNumber,
    requirePasswordSpecialCharacter: policy.requirePasswordSpecialCharacter,
    failedAttemptThreshold: policy.failedAttemptThreshold,
    failedAttemptDenialDurationSeconds: policy.failedAttemptDenialDurationSeconds,
    failedAttemptResponse: policy.failedAttemptResponse,
    enabledAuthenticationMechanisms: policy.enabledAuthenticationMechanisms,
    permittedPreAuthenticationActions: policy.permittedPreAuthenticationActions,
    expectedVersion: policy.version,
    sourceFailedAttemptThreshold: threshold,
  });
  expect(saved.status).toBe(200);
}

async function passwordLoginFrom(request: APIRequestContext, clientIp: string, username: string, attemptPassword: string, attempt: string): Promise<number> {
  const response = await request.post(`${apiOrigin}${passwordLoginPath}`, {
    data: { userNameOrEmail: username, password: attemptPassword, returnUrl: null },
    headers: { "X-Forwarded-For": clientIp, "X-Vitarish-Tenant-Host": "localhost", "X-Request-Id": `e2e-source-block-${attempt}-${Date.now()}` },
    ignoreHTTPSErrors: true,
  });
  return response.status();
}


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

async function platformAuditResponse(page: Page, event: AuditEventExpectation): Promise<{ status: number; body: unknown }> {
  return page.evaluate(async ({ origin, expected }) => {
    const url = new URL("/api/audit/platform/security-events", origin || window.location.origin);
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
  }, { origin: browserApiOrigin, expected: event });
}

async function platformAuditQuery(page: Page, event: AuditEventExpectation): Promise<AuditPage> {
  const result = await platformAuditResponse(page, event);
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
  }, { origin: browserApiOrigin, requestPath: path, requestBody: body });
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
  }, { origin: browserApiOrigin, requestPath: path, requestBody: body });
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
  }, { origin: browserApiOrigin, requestPath: path, requestBody: body });
}

// A 1x1 transparent PNG. The Files upload boundary accepts only supported
// media types and validates the content signature, not just the declared type.
const evidenceFilePngBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";
const evidenceFileName = "audit-evidence.png";
const evidenceFileSize = Buffer.from(evidenceFilePngBase64, "base64").length;

async function uploadEvidenceFile(page: Page): Promise<{ status: number; body: unknown }> {
  return page.evaluate(async ({ origin, base64, fileName }) => {
    const antiforgeryResponse = await fetch(`${origin}/api/modules/authentication/antiforgery`, { credentials: "include" });
    const antiforgery = await antiforgeryResponse.json() as { headerName: string; requestToken: string };
    const content = Uint8Array.from(atob(base64), (character) => character.charCodeAt(0));
    const digest = await crypto.subtle.digest("SHA-256", content);
    const checksum = Array.from(new Uint8Array(digest), value => value.toString(16).padStart(2, "0")).join("");
    const form = new FormData();
    form.append("content", new Blob([content], { type: "image/png" }), fileName);
    form.append("fileName", fileName);
    form.append("contentType", "image/png");
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
  }, { origin: browserApiOrigin, base64: evidenceFilePngBase64, fileName: evidenceFileName });
}

async function downloadEvidenceFile(page: Page, fileId: string): Promise<number> {
  return page.evaluate(async ({ origin, fileId, tenantHostHeader }) => {
    const response = await fetch(`${origin}/api/modules/files/${fileId}/content`, {
      credentials: "include",
      headers: { [tenantHostHeader]: window.location.host },
    });
    await response.arrayBuffer();
    return response.status;
  }, { origin: browserApiOrigin, fileId, tenantHostHeader: TENANT_HOST_HEADER });
}

// The two-host runner serves the apps in development mode; keep the framework's
// development indicator out of product evidence.
async function hideDevelopmentIndicator(page: Page): Promise<void> {
  await page.addStyleTag({ content: "nextjs-portal { display: none !important; }" }).catch(() => undefined);
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
    && (item.actorKind !== "user" || Boolean(item.actorId))
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
  await page.getByRole("textbox", { name: uiText.outcome, exact: true }).fill(event.outcome);
  const response = page.waitForResponse((candidate) =>
    candidate.url().includes("/api/audit/platform/security-events") && candidate.request().method() === "GET",
  );
  await page.getByRole("button", { name: uiText.applyFilters }).click();
  await response;
  const firstRow = page.locator("tbody tr").first();
  await expect(firstRow).toBeVisible({ timeout: 30_000 });
  await expect(firstRow.getByText(visibleEvent, { exact: true }).first()).toBeVisible();
  await expect(firstRow.getByText(event.operation, { exact: true }).last()).toBeVisible();
  await expect(firstRow.getByText(localizeSecurityAuditOutcome(locale, event.outcome, event.outcome), { exact: true })).toBeVisible();

  mkdirSync(evidenceDirectory, { recursive: true });
  await hideDevelopmentIndicator(page);
  await page.screenshot({ path: join(evidenceDirectory, filename), fullPage: true });
}

async function platformAuditCategoryQuery(page: Page, category: string, pageNumber: number): Promise<AuditPage & { page: number; pageSize: number; totalCount: number }> {
  const result = await page.evaluate(async ({ origin, eventCategory, page }) => {
    const url = new URL("/api/audit/platform/security-events", origin || window.location.origin);
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
  }, { origin: browserApiOrigin, eventCategory: category, page: pageNumber });

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
  await hideDevelopmentIndicator(page);
  await page.screenshot({
    path: join(evidenceDirectory, "authentication-catalog-page-1.png"),
    fullPage: true,
  });
}

test("real actions create visible platform security-audit evidence", async ({ page, browser, request }, testInfo) => {
  test.setTimeout(300_000);
  const fileTransferAuditKey = process.env[twoHostE2eFileTransferAuditKeyEnvironmentKey];
  if (!fileTransferAuditKey) throw new Error("The disposable file transfer audit key must be supplied by the two-host runner.");
  const evidenceFileNameHash = createHmac("sha256", fileTransferAuditKey).update(evidenceFileName, "utf8").digest("hex");
  const anonymousContext = await browser.newContext();
  const anonymousPage = await anonymousContext.newPage();
  const administratorContext = await browser.newContext();
  const administratorPage = await administratorContext.newPage();
  const temporaryPassword = `E2E-Audit-${Date.now()}-Temporary!`;
  // The session policy is one record for the whole host. Leaving the two-session limit
  // in place would refuse later specs' sign-ins with 429, so the test puts it back last:
  // restoring the one-session limit ends this administrator's session.
  let restoreSessionPolicy: (() => Promise<void>) | undefined;
  // The per-address threshold is lowered so three failures block one address; it is put back in finally.
  let restoreSourceThreshold: (() => Promise<void>) | undefined;

  try {
    await login(page, ownerUsername, ownerOrigin, ownerClientIp);

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
    }, browserApiOrigin);
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
    // Information export is audited where user data leaves the system: the file download.
    // Audit review and its CSV are queries and record nothing.
    const uploadedFileId = (uploaded.body as { file?: { id?: string } } | null)?.file?.id;
    expect(uploadedFileId).toBeTruthy();
    expect(await downloadEvidenceFile(page, uploadedFileId!)).toBe(200);

    // An unauthorized read is recorded once, naming the attempted operation; the
    // clinic owner may review only its own tenant, not the platform audit.
    const deniedReview = { ...auditEvents.auditReview, operation: "Audit_PlatformSecurityEvents_List", outcome: "denied" };
    const deniedBefore = await platformAuditQuery(administratorPage, deniedReview);
    const forbiddenStatus = await page.evaluate(async (origin) => {
      const url = new URL("/api/audit/platform/security-events", origin || window.location.origin);
      url.search = new URLSearchParams({ page: "1", pageSize: "1", sortBy: "1", descending: "true", uncataloguedOnly: "false" }).toString();
      return (await fetch(url, { credentials: "include" })).status;
    }, browserApiOrigin);
    expect(forbiddenStatus).toBe(403);
    const deniedAfter = await platformAuditQuery(administratorPage, deniedReview);
    expect(deniedAfter.totalCount).toBe(deniedBefore.totalCount + 1);
    expect(deniedAfter.items[0]?.resource).toBe("endpoint/Audit_PlatformSecurityEvents_List");

    const accessHistory = await page.evaluate(async (origin) => {
      const response = await fetch(`${origin}/api/modules/authentication/access-history`, { credentials: "include" });
      return { status: response.status, body: await response.json().catch(() => null) };
    }, browserApiOrigin);
    expect(accessHistory.status).toBe(200);
    // The access history keeps the address of the owner's sign-in (the forwarded test client IP).
    expect((accessHistory.body as { lastSuccessfulIpAddress?: string } | null)?.lastSuccessfulIpAddress).toBe(ownerClientIp);
    await page.goto(ClinicRoutes.Account.Security, { waitUntil: "domcontentloaded" });
    await expect(page.getByText(new RegExp(escapeRegExp(ownerClientIp))).first()).toBeVisible({ timeout: 30_000 });
    mkdirSync(evidenceDirectory, { recursive: true });
    await page.screenshot({ path: join(evidenceDirectory, "account-security-access-history.png"), fullPage: true });
    expect((await postJson(page, "/api/modules/authentication/access-history/acknowledge")).status).toBe(200);

    // OTP establishes a second session and may invalidate the owner session under
    // the evaluated concurrent-session policy, so perform it after owner actions.
    await completeEmailOtp(request);

    const readSessionPolicy = () => administratorPage.evaluate(async (origin) => {
      const response = await fetch(`${origin}/api/modules/authentication/security/session-policy`, { credentials: "include", cache: "no-store" });
      return { status: response.status, body: await response.json() as { maximumConcurrentSessions: number; concurrentSessionsFunctionallyRequired: boolean; defaultInactivityTimeoutMinutes: number; allowedSessionStartCidrs: string[] | null; version: number } };
    }, browserApiOrigin);
    const currentPolicy = await readSessionPolicy();
    expect(currentPolicy.status).toBe(200);
    const policy = currentPolicy.body;
    restoreSessionPolicy = async () => {
      const changedPolicy = await readSessionPolicy();
      expect(changedPolicy.status).toBe(200);
      if (changedPolicy.body.maximumConcurrentSessions === policy.maximumConcurrentSessions
        && changedPolicy.body.concurrentSessionsFunctionallyRequired === policy.concurrentSessionsFunctionallyRequired) return;
      const restoredPolicy = await putJson(administratorPage, "/api/modules/authentication/security/session-policy", {
        maximumConcurrentSessions: policy.maximumConcurrentSessions,
        concurrentSessionsFunctionallyRequired: policy.concurrentSessionsFunctionallyRequired,
        defaultInactivityTimeoutMinutes: policy.defaultInactivityTimeoutMinutes,
        allowedSessionStartCidrs: policy.allowedSessionStartCidrs,
        expectedVersion: changedPolicy.body.version,
      });
      expect(restoredPolicy.status).toBe(200);
    };
    const rejectedPolicyUpdate = await putJson(administratorPage, "/api/modules/authentication/security/session-policy", {
      maximumConcurrentSessions: policy.maximumConcurrentSessions,
      concurrentSessionsFunctionallyRequired: policy.concurrentSessionsFunctionallyRequired,
      defaultInactivityTimeoutMinutes: 129601,
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
      expect((await deniedSession).status()).toBe(429);
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
    // Repeated failures from one address, each against a different unknown name, block that
    // address: even the correct password is then refused there, with the account-lockout response.
    const originalSourceThreshold = (await readAuthenticationPolicy(administratorPage)).sourceFailedAttemptThreshold;
    restoreSourceThreshold = () => saveSourceFailedAttemptThreshold(administratorPage, originalSourceThreshold);
    await saveSourceFailedAttemptThreshold(administratorPage, 3);
    await administratorPage.goto(`${platformOrigin}${PlatformRoutes.Settings.Security}`, { waitUntil: "domcontentloaded" });
    const sourceThresholdField = administratorPage.getByRole("spinbutton", { name: uiText.sourceThreshold });
    await expect(sourceThresholdField).toHaveValue("3", { timeout: 30_000 });
    await sourceThresholdField.scrollIntoViewIfNeeded();
    mkdirSync(evidenceDirectory, { recursive: true });
    await hideDevelopmentIndicator(administratorPage);
    await administratorPage.screenshot({ path: join(evidenceDirectory, "source-threshold-setting.png") });
    // Each attempt uses its own address: a blocked address remains denied for the denial duration.
    const blockedClientIp = `203.0.113.${66 + testInfo.retry * 2}`;
    // A fresh context carries no session cookie, like an anonymous client guessing credentials.
    const anonymousRequest = await playwrightRequest.newContext({ ignoreHTTPSErrors: true });
    try {
      const sourceStatuses: number[] = [];
      for (let attempt = 0; attempt < 3; attempt += 1)
        sourceStatuses.push(await passwordLoginFrom(anonymousRequest, blockedClientIp, `e2e-unknown-source-${attempt}-${Date.now()}@example.invalid`, "E2E-Source-Wrong-Password", String(attempt)));
      expect(sourceStatuses).toEqual([401, 401, 423]);
      expect(await passwordLoginFrom(anonymousRequest, blockedClientIp, ownerUsername, password!, "correct")).toBe(423);
    } finally {
      await anonymousRequest.dispose();
    }
    const sourceBlockedRecords = await platformAuditQuery(administratorPage, { ...auditEvents.sourceBlocked, outcome: "denied" });
    expect(sourceBlockedRecords.items.some((item) =>
      item.sourceIpAddress === blockedClientIp && item.redactedReason === "source_failed_attempt_threshold" && !item.actorId,
    )).toBe(true);
    await captureAuditEvidence(administratorPage, { ...auditEvents.sourceBlocked, outcome: "denied" }, "source-address-blocked.png");
    await restoreSourceThreshold();
    restoreSourceThreshold = undefined;

    const passwordPolicyRecords = await platformAuditQuery(administratorPage, { ...auditEvents.passwordPolicyRejected, outcome: "denied" });
    expect(passwordPolicyRecords.items.every((item) => item.redactedReason === "password_policy_rejected")).toBe(true);
    await captureAuditEvidence(administratorPage, { ...auditEvents.passwordPolicyRejected, outcome: "denied" }, "password-policy-rejected.png");
    await captureAuditEvidence(administratorPage, { ...auditEvents.tenantMemberProvisioning, outcome: "succeeded" }, "tenant-member-provisioned.png");
    await captureAuditEvidence(administratorPage, { ...auditEvents.tenantMemberRemoval, outcome: "succeeded" }, "tenant-member-removed.png");
    await captureAuditEvidence(administratorPage, { ...auditEvents.tenantRoleReplacement, outcome: "succeeded" }, "tenant-role-replaced.png");
    const importRecords = await platformAuditQuery(administratorPage, { ...auditEvents.dataImport, outcome: "succeeded" });
    expect(importRecords.items.some((item) => item.resourceNameHash === evidenceFileNameHash && item.resourceExtension === "png" && item.resourceSize === evidenceFileSize), "an upload records the uploaded file's name hash, extension, and size").toBe(true);
    const exportRecords = await platformAuditQuery(administratorPage, { ...auditEvents.auditExport, outcome: "succeeded" });
    const downloadRecord = exportRecords.items.find((item) => item.resource === `files/${uploadedFileId!.replaceAll("-", "")}`);
    expect(downloadRecord?.resourceNameHash, "a download records the stored file's name hash").toBe(evidenceFileNameHash);
    expect(downloadRecord?.resourceExtension, "a download records the stored file's extension").toBe("png");
    expect(downloadRecord?.resourceSize, "a download records the stored file's size").toBe(evidenceFileSize);
    // Role changes keep the previous and new role on the record, shown in its detail sheet.
    const roleChangeRecords = await platformAuditQuery(administratorPage, { ...auditEvents.tenantRoleReplacement, outcome: "succeeded" });
    expect(roleChangeRecords.items.some((item) =>
      item.subjectId === memberId && item.changes?.some((change) => change.field === "tenantRole" && change.previousValue !== change.newValue),
    ), "the member's role replacement must carry its previous and new role").toBe(true);
    const roleEventCell = administratorPage.locator("tbody tr").first()
      .getByText(localizePlatformAuditEvent(locale, auditEvents.tenantRoleReplacement.name, auditEvents.tenantRoleReplacement.name), { exact: true }).first();
    await roleEventCell.click();
    await expect(administratorPage.getByRole("heading", { name: securityMessageCatalogs[locale].security.platformAudit.detailTitle })).toBeVisible();
    const roleChangeLine = administratorPage.getByRole("listitem").filter({ hasText: localizePlatformAuditChangeField(locale, "tenantRole", "tenantRole") });
    await expect(roleChangeLine).toBeVisible();
    // The sheet scrolls on its own, so a full-page capture would leave the change line below its fold.
    await roleChangeLine.scrollIntoViewIfNeeded();
    await hideDevelopmentIndicator(administratorPage);
    await administratorPage.screenshot({ path: join(evidenceDirectory, "tenant-role-change-detail.png") });
    await captureAuditEvidence(administratorPage, { ...auditEvents.dataImport, outcome: "succeeded" }, "data-import.png");
    const reviewEvent = { ...auditEvents.auditReview, outcome: "succeeded" };
    const reviewEventsBefore = await platformAuditQuery(administratorPage, reviewEvent);
    await administratorPage.goto(`${platformOrigin}${PlatformRoutes.Audit}`, { waitUntil: "domcontentloaded" });
    await expect(administratorPage.getByRole("heading", { name: uiText.auditTitle })).toBeVisible({ timeout: 30_000 });
    const reviewEventsAfter = await platformAuditQuery(administratorPage, reviewEvent);
    expect(reviewEventsAfter.totalCount).toBe(reviewEventsBefore.totalCount);
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

    // The threshold monitor and the Files integrity-verification background job
    // are paths a normal two-host run does not produce. The E2E host runs one
    // real monitor check while its storage-size adapter reports the threshold
    // exceeded, and exposes E2E-only triggers for the real Files verification
    // service, so both catalogued events have durable evidence in this run.
    const thresholdEvent = { ...auditEvents.auditStorageThreshold, outcome: "failed" };
    expect((await postJson(administratorPage, storageThresholdCheckPath)).status).toBe(204);
    await expect.poll(
      async () => (await platformAuditQuery(administratorPage, thresholdEvent)).items.length,
      { timeout: 60_000 },
    ).toBeGreaterThan(0);
    await captureAuditEvidence(administratorPage, thresholdEvent, "audit-storage-threshold.png");

    // While every audit-store save fails, a sign-in attempt still gets its normal answer. Once the
    // store accepts writes again, one security-failure record summarizes the outage.
    const outageSummaryPattern = /^audit_storage_outage:start=[^;]+;end=[^;]+;failed_writes=(\d+)$/;
    // The outage start outcome and recovery trigger start each fail, as do the login started/completed events.
    const refusedAuditWrites = 4;
    expect((await postJson(administratorPage, storeOutageStartPath)).status).toBe(204);
    let statusDuringOutage = 0;
    try {
      const response = await request.post(`${apiOrigin}/api/modules/authentication/login/password`, {
        data: { userNameOrEmail: `e2e-outage-${Date.now()}@example.invalid`, password: "E2E-Outage-Wrong-Password", returnUrl: null },
        headers: { "X-Forwarded-For": "203.0.113.67", "X-Vitarish-Tenant-Host": "localhost", "X-Request-Id": `e2e-audit-outage-${Date.now()}` },
        ignoreHTTPSErrors: true,
      });
      statusDuringOutage = response.status();
      const outageEvent = { ...auditEvents.securityFunctionFailure, outcome: "failed" };
      const unavailableReview = await platformAuditResponse(administratorPage, outageEvent);
      expect(unavailableReview.status, "audit review reports unavailable while its store is down").toBe(503);
    } finally {
      expect((await postJson(administratorPage, storeOutageEndPath)).status).toBe(204);
    }
    expect(statusDuringOutage).toBe(401);
    const outageEvent = { ...auditEvents.securityFunctionFailure, outcome: "failed" };
    const outageRecords = await platformAuditQuery(administratorPage, outageEvent);
    const outageSummaries = outageRecords.items.filter((item) => outageSummaryPattern.test(item.redactedReason ?? ""));
    expect(outageSummaries, "the recovered audit store must hold one summary of the outage").toHaveLength(1);
    const failedWrites = outageSummaryPattern.exec(outageSummaries[0].redactedReason ?? "");
    expect(Number(failedWrites?.[1]), "the summary counts only the writes the outage refused").toBe(refusedAuditWrites);
    await captureAuditEvidence(administratorPage, outageEvent, "audit-storage-outage.png");
    // The newest record is the summary; its detail sheet shows the outage window and the failed-write count.
    await administratorPage.locator("tbody tr").first()
      .getByText(localizePlatformAuditEvent(locale, outageEvent.name, outageEvent.name), { exact: true }).first().click();
    await expect(administratorPage.getByRole("heading", { name: uiText.eventDetails })).toBeVisible();
    const outageReason = administratorPage.getByText(/^audit_storage_outage:/);
    await expect(outageReason).toBeVisible();
    await outageReason.scrollIntoViewIfNeeded();
    await hideDevelopmentIndicator(administratorPage);
    await administratorPage.screenshot({ path: join(evidenceDirectory, "audit-storage-outage-detail.png") });
    await administratorPage.keyboard.press("Escape");

    const verifiedIntegrityEvent = { ...auditEvents.integrityVerification, outcome: "succeeded" };
    expect((await postJson(administratorPage, integrityVerificationPath(uploadedFileId!))).status).toBe(204);
    await expect.poll(
      async () => (await platformAuditQuery(administratorPage, verifiedIntegrityEvent)).items.length,
      { timeout: 30_000 },
    ).toBeGreaterThan(0);
    await captureAuditEvidence(administratorPage, verifiedIntegrityEvent, "integrity-verification.png");

    const failedIntegrityEvent = { ...auditEvents.integrityVerification, outcome: "failed" };
    expect((await postJson(administratorPage, integrityTamperPath(uploadedFileId!))).status).toBe(204);
    expect((await postJson(administratorPage, integrityVerificationPath(uploadedFileId!))).status).toBe(204);
    await expect.poll(
      async () => (await platformAuditQuery(administratorPage, failedIntegrityEvent)).items.length,
      { timeout: 30_000 },
    ).toBeGreaterThan(0);
    const failedIntegrityRecords = await platformAuditQuery(administratorPage, failedIntegrityEvent);
    expect(failedIntegrityRecords.items.every((item) => item.redactedReason === "integrity_validation_failed")).toBe(true);
    await captureAuditEvidence(administratorPage, failedIntegrityEvent, "integrity-verification-failed.png");
  } finally {
    try {
      await restoreSourceThreshold?.();
      await restoreSessionPolicy?.();
    } finally {
      await administratorPage.close();
      await administratorContext.close();
      await anonymousPage.close();
      await anonymousContext.close();
    }
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

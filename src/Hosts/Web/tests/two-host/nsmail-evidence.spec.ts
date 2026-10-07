import { expect, test, type Page } from "@playwright/test";
import { TENANT_HOST_HEADER } from "../../shared/api-client/src/apiFetch";
import { Routes } from "../../apps/private-practice-owner/src/app/routes";
import { securityMessageCatalogs } from "../../shared/security/src/localization";
import { dirname, resolve } from "node:path";
import { mkdirSync } from "node:fs";
import { twoHostE2eApiOriginEnvironmentKey } from "../../scripts/two-host-e2e-artifacts.mjs";

// Main itself, for API-boundary checks made without the browser.
const apiOrigin = process.env[twoHostE2eApiOriginEnvironmentKey] ?? "https://localhost:7272";
// In-page requests use the web origin's same-origin /api, as in production.
const browserApiOrigin = "";
const username = process.env.VITARISH_E2E_USERNAME ?? "e2e-owner";
const tenantUserUsername = "e2e-tenant-user";
const password = process.env.VITARISH_E2E_PASSWORD;
const auditEvidenceScreenshot = process.env.VITARISH_NMAIL_EVIDENCE_SCREENSHOT
  ?? resolve(process.cwd(), "..", "artifacts", "nsmail-1-evidence-md", "screenshots", "audit-authentication-password-login.png");
const screenshotDirectory = resolve(process.cwd(), "..", "artifacts", "nsmail-1-evidence-md", "screenshots");

async function login(page: Page, loginUsername = username): Promise<void> {
  expect(password, "VITARISH_E2E_PASSWORD must be supplied by the disposable E2E environment").toBeTruthy();
  await page.goto(Routes.Login.Password, { waitUntil: "domcontentloaded" });
  await page.locator('input[name="username"]').fill(loginUsername);
  await page.locator('input[name="password"]').fill(password!);
  const [response] = await Promise.all([
    page.waitForResponse((candidate) => candidate.url().endsWith("/api/modules/authentication/login/password")),
    page.locator('button[type="submit"]').click(),
  ]);
  expect(response.status()).toBe(200);
}

async function get(page: Page, path: string): Promise<{ status: number; headers: Record<string, string>; body: unknown }> {
  return page.evaluate(async ({ origin, requestPath, tenantHeader }) => {
    const response = await fetch(`${origin}${requestPath}`, {
      credentials: "include",
      headers: { [tenantHeader]: window.location.host },
    });
    let body: unknown = null;
    try { body = await response.json(); } catch { /* status is the assertion for empty responses */ }
    return { status: response.status, headers: Object.fromEntries(response.headers.entries()), body };
  }, { origin: browserApiOrigin, requestPath: path, tenantHeader: TENANT_HOST_HEADER });
}

async function captureAuditEvidence(
  page: Page,
  filename: string,
  filters: { eventName?: string; eventCategory?: string; fromDate?: string; toDate?: string } = {},
): Promise<void> {
  await page.getByLabel("Actor").fill("");
  await page.getByLabel("Event type").fill(filters.eventName ?? "");
  await page.getByLabel("Category").fill(filters.eventCategory ?? "");
  await page.getByLabel("From date").fill(filters.fromDate ?? "");
  await page.getByLabel("To date").fill(filters.toDate ?? "");
  const auditRequest = page.waitForResponse(
    (response) => response.url().includes("/api/audit/security-events") && response.request().method() === "GET",
    { timeout: 15_000 },
  ).catch(() => null);
  await page.getByRole("button", { name: "Apply filters" }).click();
  await auditRequest;
  await expect(page.locator("tbody tr").first()).toBeVisible({ timeout: 30_000 });
  const evidencePath = resolve(screenshotDirectory, filename);
  mkdirSync(dirname(evidencePath), { recursive: true });
  await page.screenshot({ path: evidencePath, fullPage: true });
}

async function captureSurface(page: Page, path: string, heading: string, filename: string): Promise<void> {
  await page.goto(path, { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { name: heading, exact: true })).toBeVisible({ timeout: 30_000 });
  const evidencePath = resolve(screenshotDirectory, filename);
  mkdirSync(dirname(evidencePath), { recursive: true });
  await page.screenshot({ path: evidencePath, fullPage: true });
}

test.describe("NSMAIL-1 evidence execution coverage", () => {
  test("AUD-09: authorized audit review exposes filtered, attributable records", async ({ page }) => {
    test.setTimeout(300_000);
    await login(page);
    const initialAuditRequest = page.waitForResponse(
      (response) => response.url().includes("/api/audit/security-events") && response.request().method() === "GET",
      { timeout: 30_000 },
    ).catch(() => null);
    await page.goto(Routes.Clinic.Settings.Audit, { waitUntil: "domcontentloaded" });
    await expect(page.getByRole("heading", { name: "Security audit", exact: true })).toBeVisible({ timeout: 30_000 });
    await initialAuditRequest;
    await expect(page.locator("tbody tr").first()).toBeVisible({ timeout: 30_000 });
    const review = await get(page, "/api/audit/security-events?page=1&pageSize=25&sortBy=1&descending=true");
    expect(review.status).toBe(200);
    expect(review.body).toMatchObject({ items: expect.any(Array), capacityThresholdReached: expect.any(Boolean) });
    expect((review.body as { items: unknown[] }).items.length).toBeGreaterThan(0);

    await page.getByLabel("Event type").fill("authentication.password.login");
    await page.getByLabel("Category").fill("authentication");
    await page.getByRole("button", { name: "Apply filters" }).click();
    await expect(page.locator("tbody tr").first()).toBeVisible();
    const filteredReview = await get(page, "/api/audit/security-events?page=1&pageSize=25&eventName=authentication.password.login&eventCategory=authentication&sortBy=1&descending=true");
    expect(filteredReview.status).toBe(200);
    const filteredItems = (filteredReview.body as { items: Array<{ eventName?: string; eventCategory?: string }> }).items;
    expect(filteredItems.length).toBeGreaterThan(0);
    expect(filteredItems.every((item) => item.eventName === "authentication.password.login" && item.eventCategory === "authentication")).toBe(true);

    mkdirSync(dirname(auditEvidenceScreenshot), { recursive: true });
    await page.screenshot({ path: auditEvidenceScreenshot, fullPage: true });
    await captureAuditEvidence(page, "02-audit-date-scoped-records.png", {
      eventName: "authentication.password.login",
      eventCategory: "authentication",
      fromDate: new Date().toISOString().slice(0, 10),
      toDate: new Date().toISOString().slice(0, 10),
    });

    await page.getByLabel("Event type").fill("authentication.password.login");
    await page.getByLabel("Category").fill("authentication");
    await page.getByRole("button", { name: "Sort by" }).click();
    await page.getByRole("menuitem", { name: securityMessageCatalogs.en.security.audit.sortActor }).click();
    await page.getByRole("button", { name: "Apply filters" }).click();
    await expect(page.locator("tbody tr").first()).toBeVisible({ timeout: 30_000 });
    const sortedPath = resolve(screenshotDirectory, "03-audit-sorted-records.png");
    await page.screenshot({ path: sortedPath, fullPage: true });

    await captureSurface(page, Routes.Account.Security, "Security history", "04-account-security-history.png");
    await captureSurface(page, Routes.Account.Password, "Change Password", "05-account-password-controls.png");
    await captureSurface(page, Routes.Account.Passkeys, "Passkeys", "06-account-passkey-enrollment.png");

    await captureSurface(page, Routes.Clinic.Settings.Index, "Clinic Settings", "07-authenticated-clinic-settings.png");

    const healthResponse = await page.goto(`${apiOrigin}/api/modules/appointments/health`, { waitUntil: "domcontentloaded" });
    expect(healthResponse?.status()).toBe(200);
    await page.screenshot({ path: resolve(screenshotDirectory, "08-https-health-response.png"), fullPage: true });

    const anonymousContext = await page.context().browser()!.newContext();
    const anonymousPage = await anonymousContext.newPage();
    try {
      await captureSurface(anonymousPage, Routes.Login.Password, "Sign In", "09-password-authentication-entry.png");
    } finally {
      await anonymousContext.close();
    }
  });

  test("IAM-04/IAM-06: unauthenticated access is denied at the real API boundary", async ({ request }) => {
    const response = await request.get(`${apiOrigin}/api/modules/authentication/session`, {
      headers: { [TENANT_HOST_HEADER]: "localhost" },
      ignoreHTTPSErrors: true,
    });
    expect(response.status()).toBe(401);
  });

  test("DATA-11/TLS-01: plaintext ingress is redirected to HTTPS", async ({ request }) => {
    const response = await request.get(`${apiOrigin}/api/modules/appointments/health`, {
      headers: { "X-Forwarded-For": "127.0.0.1", "X-Forwarded-Host": "app.localhost", "X-Forwarded-Proto": "http" },
      maxRedirects: 0,
      ignoreHTTPSErrors: true,
    });
    expect([301, 307, 308]).toContain(response.status());
    expect(response.headers().location).toMatch(/^https:\/\//i);
  });

  test("HARD-03: authenticated session cookies are secure", async ({ page }) => {
    await login(page);
    const cookies = await page.context().cookies();
    const apiHost = new URL(apiOrigin).hostname;
    const authCookie = cookies.find((cookie) => /identity\.application/i.test(cookie.name));
    expect(authCookie, "the authentication cookie must be issued").toBeDefined();
    expect(authCookie!.sameSite).toBe("Lax");
    expect(authCookie!.expires).toBe(-1);
    expect(authCookie!.secure).toBe(true);
    expect(authCookie!.httpOnly).toBe(true);
    expect(authCookie!.domain).toBe(apiHost);
    const antiforgeryCookie = cookies.find((cookie) => /antiforgery/i.test(cookie.name));
    expect(antiforgeryCookie, "the antiforgery cookie must be issued").toBeDefined();
    expect(antiforgeryCookie!.sameSite).toBe("Lax");
    expect(antiforgeryCookie!.secure).toBe(true);
    expect(antiforgeryCookie!.httpOnly).toBe(true);
  });

  test.fixme("HARD-02: security headers are present on the exact release responses", async () => {
    // Strict-Transport-Security, Cache-Control, Pragma, X-Content-Type-Options,
    // X-Frame-Options, and X-XSS-Protection are owned by
    // src/Hosts/deployment/Caddyfile, which this two-host E2E topology does not
    // run. The production deployment asserts them on document, /api/, and
    // /_next/static/ responses through
    // src/Hosts/Web/scripts/capture-production-browser-evidence.mjs, so this
    // case stays skipped here without dropping its requirement trace.
  });

  test("HARD-05/HARD-06: state-changing requests require the trusted method and CSRF boundary", async ({ page }) => {
    await login(page);
    const response = await page.evaluate(async ({ origin, tenantHeader }) => {
      const result = await fetch(`${origin}/api/modules/clinic/patients/count`, {
        method: "POST",
        credentials: "include",
        headers: { "Content-Type": "application/json", [tenantHeader]: window.location.host },
        body: JSON.stringify({}),
      });
      return { status: result.status, body: await result.json() as { code?: string } };
    }, { origin: browserApiOrigin, tenantHeader: TENANT_HOST_HEADER });
    expect(response.status).toBe(400);
    expect(response.body.code).toBe("AntiforgeryValidationFailed");
  });

  test("IAM-01/IAM-02: rejected password authentication is visible without identity disclosure", async ({ page }) => {
    await page.goto(Routes.Login.Password, { waitUntil: "domcontentloaded" });
    await page.getByLabel("Username or Email").fill("unknown-user@example.invalid");
    await page.getByLabel("Password").fill("incorrect-e2e-password");
    const response = page.waitForResponse(
      (candidate) => candidate.url().endsWith("/api/modules/authentication/login/password"),
    );
    await page.getByRole("button", { name: "Sign In" }).click();
    expect([400, 401]).toContain((await response).status());
    await expect(page.getByText("Invalid username or password", { exact: true })).toBeVisible();
    await page.screenshot({ path: resolve(screenshotDirectory, "10-password-authentication-failure.png"), fullPage: true });
  });

  test("FIA_AFL/FIA_PMG_EXT: failed attempt history and password validation are visible in account security", async ({ page }) => {
    await page.goto(Routes.Login.Password, { waitUntil: "domcontentloaded" });
    await page.getByLabel("Username or Email").fill(tenantUserUsername);
    await page.getByLabel("Password").fill("incorrect-e2e-password");
    const response = page.waitForResponse(
      (candidate) => candidate.url().endsWith("/api/modules/authentication/login/password"),
    );
    await page.getByRole("button", { name: "Sign In" }).click();
    expect([400, 401]).toContain((await response).status());
    await expect(page.getByText("Invalid username or password", { exact: true })).toBeVisible();

    await login(page, tenantUserUsername);
    await page.goto(Routes.Account.Security, { waitUntil: "domcontentloaded" });
    await expect(page.getByRole("heading", { name: "Security history", exact: true })).toBeVisible({ timeout: 30_000 });
    await expect(page.getByText("Most recent unsuccessful sign-in", { exact: true })).toBeVisible();
    await expect(page.getByText("Current unsuccessful attempts", { exact: true })).toBeVisible();
    await page.screenshot({ path: resolve(screenshotDirectory, "11-account-failed-attempt-history.png"), fullPage: true });

    await page.goto(Routes.Account.Password, { waitUntil: "domcontentloaded" });
    await expect(page.getByRole("heading", { name: "Change Password", exact: true })).toBeVisible({ timeout: 30_000 });
    await page.locator('input[name="currentPassword"]').fill(password!);
    await page.locator('input[name="newPassword"]').fill("short");
    await page.locator('input[name="confirmPassword"]').fill("different");
    await page.getByRole("button", { name: "Change Password" }).click();
    await expect(page.getByText("Passwords do not match", { exact: true })).toBeVisible();
    await page.screenshot({ path: resolve(screenshotDirectory, "12-password-policy-validation.png"), fullPage: true });
  });

  test("FTA_SSL-04: authenticated users reach the explicit sign-out confirmation", async ({ page }) => {
    await login(page);
    await page.goto(Routes.Logout, { waitUntil: "domcontentloaded" });
    await expect(page.getByRole("heading", { name: "Sign Out", exact: true })).toBeVisible({ timeout: 30_000 });
    await expect(page.getByText("Are you sure you want to sign out?", { exact: true })).toBeVisible();
    await page.screenshot({ path: resolve(screenshotDirectory, "13-session-termination-confirmation.png"), fullPage: true });
  });

  test("FMT_SMF-01: clinic user-management entry point exposes tenant member controls", async ({ page }) => {
    await login(page);
    await page.goto(Routes.Clinic.Settings.Users, { waitUntil: "domcontentloaded" });
    await expect(page.getByRole("heading", { name: "Clinic Users", exact: true })).toBeVisible({ timeout: 30_000 });
    await expect(page.getByRole("heading", { name: "Add staff member", exact: true })).toBeVisible();
    await expect(page.getByRole("list", { name: "Tenant memberships", exact: true })).toBeVisible();
    await page.screenshot({ path: resolve(screenshotDirectory, "14-clinic-user-management-state.png"), fullPage: true });
  });

  const specificationCases: Array<[string, string]> = [
    ["AUD-01..AUD-08", "FAU_GEN.1.1; FAU_STG.1.1; FAU_STG.1.2; FAU_STG.3.1; FAU_STG.4.1; FCS_COP.1.1(1); FCS_COP.1.1(2); FCS_COP.1.1(3); FCS_CKM.4.1; FCS_CKM.1.1; FCS_COP.1.1(4)"],
    ["IAM-01..IAM-03", "FIA_AFL.1.1; FIA_AFL.1.2; FIA_ATD.1.1; FIA_PMG_EXT.1.1"],
    ["IAM-05..IAM-09", "FIA_UAU.5.1; FIA_UAU.5.2; FIA_USB.1.1; FIA_USB.1.2; FIA_USB.1.3; FDP_ACC.1.1; FDP_ACF.1.1; FDP_ACF.1.2; FDP_ACF.1.3; FDP_ACF.1.4; FDP_RIP.2.1; FMT_MOF.1.1; FMT_MSA.1.1; FMT_MSA.3.1; FMT_MSA.3.2; FMT_MTD.1.1(1); FMT_MTD.1.1(2); FMT_SMF.1.1; FMT_SMR.1.1; FMT_SMR.1.2"],
    ["DATA-01..DATA-10", "FDP_ITC.2.1; FDP_ITC.2.2; FDP_ITC.2.3; FDP_ITC.2.4; FDP_ITC.2.5; FDP_ETC.2.1; FDP_ETC.2.2; FDP_ETC.2.3; FDP_ETC.2.4; FDP_SDI.2.1; FDP_SDI.2.2; FPT_FLS.1.1; FPT_ITT.1.1; FPT_TDC.1.1; FPT_TDC.1.2; FPT_STM.1.1; FPT_TUD_EXT.1.2; FPT_TUD_EXT.1.3; FRU_FLT.1.1; FTA_MCS.1.1; FTA_MCS.1.2; FTA_SSL.3.1; FTA_SSL.4.1; FTA_TAH.1.1; FTA_TAH.1.2; FTA_TAH.1.3; FTA_TSE.1.1"],
    ["TLS-02..TLS-05", "FCS_HTTPS_EXT.1.2; FCS_HTTPS_EXT.1.3; FCS_TLSC_EXT.1.1; FCS_TLSC_EXT.1.2; FCS_TLSC_EXT.1.3; FCS_TLSC_EXT.1.4; FCS_TLSS_EXT.1.1; FCS_TLSS_EXT.1.2; FCS_TLSS_EXT.1.3; FCS_TLSS_EXT.2.4; FCS_TLSS_EXT.2.6; FIA_X509_EXT.1.1/Rev; FIA_X509_EXT.1.2/Rev; FIA_X509_EXT.2.1; FCS_SSH_EXT.1.1..FCS_SSH_EXT.1.9"],
    ["HARD-01/HARD-04", "Tools, frameworks, technologies, and versions; Unused ports"],
  ];

  for (const [caseIds, requirements] of specificationCases) {
    test.fixme(`${caseIds}: ${requirements}`, async () => {
      // These cases require product-specific fixtures, release configuration,
      // fault injection, operator channels, or an exact-host scanner. They
      // remain visible in the report until their controlled setup is supplied.
    });
  }
});

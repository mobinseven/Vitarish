import { expect, test, type Browser, type Page } from "@playwright/test";
import { existsSync, mkdirSync, readFileSync, statSync } from "node:fs";
import { join, resolve } from "node:path";
import { Routes as ClinicRoutes } from "../../apps/private-practice-owner/src/app/routes";
import { Routes as PlatformRoutes } from "../../apps/platform-admin/src/app/routes";
import { clinicMessageCatalogs } from "../../shared/clinic/src/localization/catalogs";
import { localeCookieName } from "../../shared/localization/src/localeCookie";
import { usersMessageCatalogs } from "../../shared/users/src/localization/catalogs";
import { TENANT_HOST_HEADER } from "../../shared/api-client/src/apiFetch";

// Captures the product screens the NSMAIL-1 self-assessment document shows, in
// the Persian UI, one file per document slot group. It is an evidence tool, not a
// regression test: it signs accounts in and leaves history behind, so it runs only
// when explicitly requested, on its own:
//   VITARISH_NSMAIL_DOCUMENT_EVIDENCE=1 VITARISH_E2E_GREP="NSMAIL document evidence" node scripts/run-two-host-e2e.mjs
const webOrigin = process.env.PLAYWRIGHT_BASE_URL ?? "http://localhost:3005";
const platformOrigin = process.env.PLAYWRIGHT_PLATFORM_ADMIN_ORIGIN ?? "http://localhost:3006";
const password = process.env.VITARISH_E2E_PASSWORD;
const ownerUsername = process.env.VITARISH_E2E_USERNAME ?? "e2e-owner";
const tenantUserUsername = "e2e-tenant-user";
const platformAdminUsername = process.env.VITARISH_E2E_USER_MANAGEMENT_PLATFORM_ADMIN_USERNAME ?? "e2e-user-management-admin";
const evidenceDirectory = resolve(process.env.VITARISH_NSMAIL_DOCUMENT_EVIDENCE_DIR ?? join(process.cwd(), "artifacts", "nsmail-document-evidence"));
const usersText = usersMessageCatalogs.fa.users.management;
const smtpCapturePath = process.env.VITARISH_E2E_SMTP_CAPTURE_PATH;
const clinicText = clinicMessageCatalogs.fa.clinic;
const procedureItemsText = clinicText.procedures.items;
const newVisitText = clinicText.patient.quickActions.newVisit;
const newVisitDialogText = clinicText.procedures.visits.dialog;
// The alert is the localized refusal followed by the accepted types; matching its
// opening proves the picker policy rejected the file.
const rejectedTypeMessage = procedureItemsText.fileTypeNotAllowed.split("{types}")[0].trim();
// E2eFixtureSeeder seeds this patient for tenant A; it has no procedure or event yet.
const fixturePatientId = "2d7a3eb9-1d47-4c63-9a45-19d7e6a0fb18";
// A real 1x1 PNG, so the allowlist's declared type, extension and leading signature all pass.
const pngBytes = [
  0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A,
  0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52,
  0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01,
  0x08, 0x06, 0x00, 0x00, 0x00, 0x1F, 0x15, 0xC4,
  0x89, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x44, 0x41,
  0x54, 0x78, 0xDA, 0x63, 0x64, 0x60, 0xF8, 0x5F,
  0x0F, 0x00, 0x02, 0x87, 0x01, 0x80, 0xEB, 0x47,
  0xBA, 0x92, 0x00, 0x00, 0x00, 0x00, 0x49, 0x45,
  0x4E, 0x44, 0xAE, 0x42, 0x60, 0x82,
];
// The leading MZ bytes of a Windows executable; the composer rejects the .exe
// extension against the upload policy before anything is sent.
const executableBytes = [0x4D, 0x5A, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00];

test.describe.configure({ mode: "serial" });

async function persianContext(browser: Browser, clientIp: string) {
  const context = await browser.newContext({
    locale: "fa-IR",
    viewport: { width: 1440, height: 900 },
    extraHTTPHeaders: { "X-Forwarded-For": clientIp },
  });
  await context.addCookies([
    { name: localeCookieName, value: "fa", url: `${webOrigin}/` },
    { name: localeCookieName, value: "fa", url: `${platformOrigin}/` },
  ]);
  return context;
}

async function signIn(page: Page, origin: string, username: string, loginPath: string): Promise<void> {
  expect(password, "VITARISH_E2E_PASSWORD must be supplied by the disposable E2E environment").toBeTruthy();
  await page.goto(`${origin}${loginPath}`, { waitUntil: "domcontentloaded" });
  await expect(page.locator('button[type="submit"]')).toBeEnabled({ timeout: 30_000 });
  await page.locator('input[name="username"]').fill(username);
  await page.locator('input[name="password"]').fill(password!);
  const [response] = await Promise.all([
    page.waitForResponse((candidate) => candidate.url().endsWith("/api/modules/authentication/login/password")),
    page.locator('button[type="submit"]').click(),
  ]);
  expect(response.status()).toBe(200);
}

async function capture(page: Page, name: string, fullPage = true): Promise<void> {
  mkdirSync(evidenceDirectory, { recursive: true });
  await page.waitForLoadState("networkidle").catch(() => undefined);
  // The two-host runner serves the apps in development mode; hide the framework's
  // development indicator so it never appears in product evidence.
  await page.addStyleTag({ content: "nextjs-portal { display: none !important; }" }).catch(() => undefined);
  // Let enter transitions (for example a dialog fading in) settle; endless spinners are ignored.
  await page.evaluate(() => Promise.all(document.getAnimations()
    .filter((animation) => animation.effect?.getComputedTiming().iterations !== Infinity)
    .map((animation) => animation.finished.catch(() => undefined))));
  await page.screenshot({ path: join(evidenceDirectory, `${name}.png`), fullPage });
}

async function openPage(page: Page, url: string, name: string): Promise<void> {
  await page.goto(url, { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading").first()).toBeVisible({ timeout: 30_000 });
  await capture(page, name);
}

async function openUserDetail(page: Page, username: string): Promise<void> {
  await page.goto(`${platformOrigin}${PlatformRoutes.Users.Index}`, { waitUntil: "domcontentloaded" });
  const search = page.getByPlaceholder(usersText.searchPlaceholder);
  await expect(search).toBeVisible({ timeout: 30_000 });
  await search.fill(username);
  await page.getByRole("button", { name: usersText.search }).click();
  const row = page.locator("tbody tr").filter({ hasText: username }).first();
  await expect(row).toBeVisible({ timeout: 30_000 });
  await row.getByRole("button").first().click();
  await expect(page.getByRole("heading", { name: usersText.accountStatusTitle })).toBeVisible({ timeout: 30_000 });
}

async function renderEvidenceTable(page: Page, title: string, headings: string[], rows: string[][], name: string): Promise<void> {
  const escape = (value: string) => value.replace(/[&<>"]/g, (character) => `&#${character.charCodeAt(0)};`);
  const header = headings.map((heading) => `<th>${escape(heading)}</th>`).join("");
  const body = rows.map((row) => `<tr>${row.map((cell) => `<td>${escape(cell)}</td>`).join("")}</tr>`).join("");
  await page.setContent(`<!doctype html><html dir="ltr"><head><meta charset="utf-8"><style>body{margin:0;padding:32px;background:#f4f6f8;font:15px/1.5 Arial,sans-serif;color:#17212b}main{background:#fff;border:1px solid #d8dee5;border-radius:12px;padding:24px}h1{font-size:22px;margin:0 0 16px}table{width:100%;border-collapse:collapse}th,td{text-align:left;padding:10px 12px;border-bottom:1px solid #e4e8ed;overflow-wrap:anywhere}th{background:#edf1f5}</style></head><body><main><h1>${escape(title)}</h1><table><thead><tr>${header}</tr></thead><tbody>${body}</tbody></table></main></body></html>`);
  await capture(page, name);
}

test.describe("NSMAIL document evidence", () => {
  test.skip(process.env.VITARISH_NSMAIL_DOCUMENT_EVIDENCE !== "1", "Evidence capture runs only when explicitly requested.");

  test("platform administrator screens", async ({ browser }) => {
    test.setTimeout(300_000);
    const context = await persianContext(browser, "198.51.100.240");
    const page = await context.newPage();
    try {
      await signIn(page, platformOrigin, platformAdminUsername, PlatformRoutes.LoginPassword);
      await openPage(page, `${platformOrigin}${PlatformRoutes.Settings.Security}`, "admin-security-settings");
      await openPage(page, `${platformOrigin}${PlatformRoutes.Audit}`, "admin-audit");
      await openPage(page, `${platformOrigin}${PlatformRoutes.Users.Index}`, "admin-users");
      await openUserDetail(page, ownerUsername);
      await capture(page, "admin-user-detail");
      await openPage(page, `${platformOrigin}${PlatformRoutes.Tenants.Index}`, "admin-tenants");
    } finally {
      await context.close();
    }
  });

  test("clinic owner screens", async ({ browser }) => {
    test.setTimeout(300_000);
    const context = await persianContext(browser, "198.51.100.241");
    const page = await context.newPage();
    try {
      await signIn(page, webOrigin, ownerUsername, ClinicRoutes.Login.Password);
      await openPage(page, `${webOrigin}${ClinicRoutes.Clinic.Settings.Users}`, "clinic-users");
      await openPage(page, `${webOrigin}${ClinicRoutes.Clinic.Settings.Audit}`, "clinic-audit");
      await openPage(page, `${webOrigin}${ClinicRoutes.Account.Security}`, "account-security");
      await openPage(page, `${webOrigin}${ClinicRoutes.Account.Index}`, "account-overview");

      await page.goto(`${webOrigin}${ClinicRoutes.Account.Password}`, { waitUntil: "domcontentloaded" });
      await expect(page.locator('input[name="newPassword"]')).toBeVisible({ timeout: 30_000 });
      await page.locator('input[name="currentPassword"]').fill(password!);
      await page.locator('input[name="newPassword"]').fill("short");
      await page.locator('input[name="confirmPassword"]').fill("short");
      const [policyResponse] = await Promise.all([
        page.waitForResponse((candidate) => candidate.request().method() === "POST" && candidate.url().includes("/password")),
        page.locator('button[type="submit"]').last().click(),
      ]);
      expect(policyResponse.status()).toBe(400);
      await expect(page.locator('[data-slot="alert-content"]').first()).toBeVisible({ timeout: 30_000 });
      await capture(page, "password-policy-validation");

      const cookies = await context.cookies();
      await renderEvidenceTable(
        page,
        "Session cookie attributes (browser cookie store)",
        ["Name", "Domain", "Path", "Secure", "HttpOnly", "SameSite", "Expires"],
        cookies
          .filter((cookie) => /identity|antiforgery|session/i.test(cookie.name))
          .map((cookie) => [cookie.name, cookie.domain, cookie.path, String(cookie.secure), String(cookie.httpOnly), cookie.sameSite, cookie.expires === -1 ? "Session (browser close)" : new Date(cookie.expires * 1000).toISOString()]),
        "cookie-attributes",
      );

      await openPage(page, `${webOrigin}${ClinicRoutes.Logout}`, "sign-out-confirmation");
    } finally {
      await context.close();
    }
  });

  test("sign-in, denial and failure screens", async ({ browser }) => {
    test.setTimeout(300_000);
    const context = await persianContext(browser, "198.51.100.242");
    const page = await context.newPage();
    try {
      await openPage(page, `${webOrigin}${ClinicRoutes.Login.Index}`, "login-methods");

      await page.goto(`${webOrigin}${ClinicRoutes.Login.Password}`, { waitUntil: "domcontentloaded" });
      await expect(page.locator('button[type="submit"]')).toBeEnabled({ timeout: 30_000 });
      await page.locator('input[name="username"]').fill("nsmail-unknown-user@example.invalid");
      await page.locator('input[name="password"]').fill("incorrect-e2e-password");
      await Promise.all([
        page.waitForResponse((candidate) => candidate.url().endsWith("/api/modules/authentication/login/password")),
        page.locator('button[type="submit"]').click(),
      ]);
      await capture(page, "login-failed");

      // The threshold is 5 by default; keep failing until the denial response
      // appears, which looks the same for known and unknown names.
      let lastStatus = 0;
      for (let attempt = 0; attempt < 8 && lastStatus !== 429 && lastStatus !== 423; attempt++) {
        await page.locator('input[name="password"]').fill(`incorrect-e2e-password-${attempt}`);
        const [response] = await Promise.all([
          page.waitForResponse((candidate) => candidate.url().endsWith("/api/modules/authentication/login/password")),
          page.locator('button[type="submit"]').click(),
        ]);
        lastStatus = response.status();
      }
      await capture(page, "login-locked-out");
    } finally {
      await context.close();
    }

    const tenantContext = await persianContext(browser, "198.51.100.243");
    const tenantPage = await tenantContext.newPage();
    try {
      await signIn(tenantPage, webOrigin, tenantUserUsername, ClinicRoutes.Login.Password);
      await tenantPage.goto(`${webOrigin}${ClinicRoutes.Clinic.Settings.Audit}`, { waitUntil: "domcontentloaded" });
      await capture(tenantPage, "audit-forbidden-for-tenant-user");

      const requests = await tenantPage.evaluate(async (tenantHeader) => {
        const host = window.location.host;
        const withoutToken = await fetch("/api/modules/clinic/patients/count", {
          method: "POST",
          credentials: "include",
          headers: { "Content-Type": "application/json", [tenantHeader]: host },
          body: JSON.stringify({}),
        });
        const malformed = await fetch("/api/modules/authentication/antiforgery", { credentials: "include" })
          .then((response) => response.json() as Promise<{ headerName: string; requestToken: string }>)
          .then((token) => fetch("/api/modules/clinic/patients/count", {
            method: "POST",
            credentials: "include",
            headers: { "Content-Type": "application/json", [tenantHeader]: host, [token.headerName]: token.requestToken },
            body: "{ \"broken\": ",
          }));
        return [
          { name: "POST without anti-CSRF token", status: withoutToken.status, body: await withoutToken.text() },
          { name: "POST with malformed JSON body", status: malformed.status, body: await malformed.text() },
        ];
      }, TENANT_HOST_HEADER);
      await renderEvidenceTable(
        tenantPage,
        "Rejected requests: anti-CSRF and malformed input (no server error, no stack trace)",
        ["Request", "HTTP status", "Response body"],
        requests.map((request) => [request.name, String(request.status), request.body.slice(0, 400) || "(empty body)"]),
        "rejected-requests",
      );
    } finally {
      await tenantContext.close();
    }
  });

  test("account status and access denial screens", async ({ browser }) => {
    test.setTimeout(300_000);
    const adminContext = await persianContext(browser, "198.51.100.244");
    const admin = await adminContext.newPage();
    const userContext = await persianContext(browser, "198.51.100.245");
    const user = await userContext.newPage();
    try {
      await signIn(admin, platformOrigin, platformAdminUsername, PlatformRoutes.LoginPassword);
      await openUserDetail(admin, tenantUserUsername);
      await admin.getByRole("button", { name: usersText.endSessions }).click();
      await admin.getByRole("dialog").getByRole("button", { name: usersText.endSessionsConfirmAction }).click();
      await expect(admin.getByRole("dialog")).toBeHidden();
      await admin.getByRole("button", { name: usersText.disableAccount }).click();
      // A modal covers the viewport; a full-page capture would show the page behind it unmasked.
      await capture(admin, "admin-disable-account-dialog", false);
      await admin.getByRole("dialog").getByRole("button", { name: usersText.disableAccountConfirmAction }).click();
      await expect(admin.getByText(usersText.accountDisabled).first()).toBeVisible({ timeout: 30_000 });
      await capture(admin, "admin-account-disabled");

      await user.goto(`${webOrigin}${ClinicRoutes.Login.Password}`, { waitUntil: "domcontentloaded" });
      await expect(user.locator('button[type="submit"]')).toBeEnabled({ timeout: 30_000 });
      await user.locator('input[name="username"]').fill(tenantUserUsername);
      await user.locator('input[name="password"]').fill(password!);
      const [disabledLogin] = await Promise.all([
        user.waitForResponse((candidate) => candidate.url().endsWith("/api/modules/authentication/login/password")),
        user.locator('button[type="submit"]').click(),
      ]);
      expect(disabledLogin.status()).toBe(403);
      await capture(user, "login-account-disabled");
    } finally {
      await admin.getByRole("button", { name: usersText.enableAccount }).click().catch(() => undefined);
      await admin.getByRole("dialog").getByRole("button", { name: usersText.enableAccountConfirmAction }).click().catch(() => undefined);
      await adminContext.close();
      await userContext.close();
    }

    // Email-code sign-in creates a new account with no role or membership.
    expect(smtpCapturePath, "VITARISH_E2E_SMTP_CAPTURE_PATH must be supplied by the two-host runner").toBeTruthy();
    const newcomerContext = await persianContext(browser, "198.51.100.246");
    const newcomer = await newcomerContext.newPage();
    try {
      await newcomer.goto(`${webOrigin}${ClinicRoutes.Login.Email}`, { waitUntil: "domcontentloaded" });
      const email = newcomer.locator('input[type="email"]').first();
      await expect(email).toBeVisible({ timeout: 30_000 });
      await email.fill(`nsmail-no-access-${Date.now()}@example.test`);
      const previous = existsSync(smtpCapturePath!) ? statSync(smtpCapturePath!).mtimeMs : 0;
      await Promise.all([
        newcomer.waitForResponse((candidate) => candidate.url().endsWith("/api/modules/authentication/login/email/begin")),
        newcomer.locator('button[type="submit"]').first().click(),
      ]);
      await expect.poll(() => existsSync(smtpCapturePath!) && statSync(smtpCapturePath!).mtimeMs > previous).toBe(true);
      const code = /Your verification code is:\s*(?:<strong>)?([^<\s]+)/i.exec(readFileSync(smtpCapturePath!, "utf8"))?.[1];
      expect(code).toEqual(expect.any(String));
      await newcomer.locator('input[autocomplete="one-time-code"], input[name="code"]').first().fill(code!);
      const [complete] = await Promise.all([
        newcomer.waitForResponse((candidate) => candidate.url().endsWith("/api/modules/authentication/login/email/complete")),
        newcomer.locator('button[type="submit"]').first().click(),
      ]);
      expect(complete.status()).toBe(403);
      await capture(newcomer, "login-no-access");
    } finally {
      await newcomerContext.close();
    }
  });

  test("audit records of idle expiry, administrator termination and disabled sign-in", async ({ browser }) => {
    test.setTimeout(300_000);
    const adminContext = await persianContext(browser, "198.51.100.247");
    const admin = await adminContext.newPage();
    const userContext = await persianContext(browser, "198.51.100.248");
    const user = await userContext.newPage();
    const inactivity = admin.getByLabel(usersText.inactivityTimeout);
    try {
      await signIn(admin, platformOrigin, platformAdminUsername, PlatformRoutes.LoginPassword);
      await openUserDetail(admin, tenantUserUsername);
      await inactivity.fill("1");
      await admin.getByRole("button", { name: usersText.saveInactivity }).click();
      await expect(admin.getByText(usersText.inactivitySaved)).toBeVisible({ timeout: 30_000 });

      await signIn(user, webOrigin, tenantUserUsername, ClinicRoutes.Login.Password);
      // Idle past the one-minute override; the next request ends the session.
      await user.waitForTimeout(70_000);
      const expired = await user.evaluate(async () => (await fetch("/api/modules/authentication/session", { credentials: "include" })).status);
      expect(expired).toBe(401);
    } finally {
      await inactivity.fill("").catch(() => undefined);
      await admin.getByRole("button", { name: usersText.saveInactivity }).click().catch(() => undefined);
      await userContext.close();
    }
    try {
      await openPage(admin, `${platformOrigin}${PlatformRoutes.Audit}`, "admin-audit-latest");
    } finally {
      await adminContext.close();
    }
  });

  test("clinic procedure attachment upload checks", async ({ browser }) => {
    test.setTimeout(300_000);
    const context = await persianContext(browser, "198.51.100.249");
    const page = await context.newPage();
    try {
      await signIn(page, webOrigin, ownerUsername, ClinicRoutes.Login.Password);

      // The fixture patient (seeded as its procedure project); opening the overview creates its
      // "Visits" procedure, and the new-visit action creates the event the composer belongs to.
      const patientId = fixturePatientId;
      await page.goto(`${webOrigin}${ClinicRoutes.Clinic.Patients.Detail(patientId)}`, { waitUntil: "domcontentloaded" });
      const newVisit = page.getByRole("button", { name: newVisitText }).first();
      await expect(newVisit).toBeEnabled({ timeout: 30_000 });
      await newVisit.click();

      const dialog = page.getByRole("dialog");
      await expect(dialog.getByRole("heading", { name: newVisitDialogText.title })).toBeVisible({ timeout: 30_000 });
      const [eventCreation] = await Promise.all([
        page.waitForResponse((candidate) => candidate.request().method() === "POST" && candidate.url().endsWith("/api/modules/sea/events/after")),
        dialog.getByRole("button", { name: newVisitDialogText.create }).click(),
      ]);
      expect(eventCreation.status()).toBe(200);
      const createdEvent = (await eventCreation.json()) as { entityId?: string };
      expect(createdEvent.entityId).toEqual(expect.any(String));
      await expect(page).toHaveURL(new RegExp(`/patients/${patientId}/procedures/[^/]+/events/[^/]+$`), { timeout: 30_000 });

      // The composer mounts only when the event grants write, and its picker filter
      // comes from the server's upload policy, so the accept attribute proves both.
      const fileInput = page.locator('input[type="file"]');
      await expect(fileInput).toHaveAttribute("accept", /image\/png/, { timeout: 30_000 });

      await fileInput.setInputFiles({
        name: "setup.exe",
        mimeType: "application/x-msdownload",
        buffer: Buffer.from(executableBytes),
      });
      const rejectionAlert = page.getByRole("alert").filter({ hasText: rejectedTypeMessage });
      await expect(rejectionAlert).toBeVisible({ timeout: 30_000 });
      await rejectionAlert.scrollIntoViewIfNeeded();
      await capture(page, "upload-type-rejected", false);

      await fileInput.setInputFiles({
        name: "scan.png",
        mimeType: "image/png",
        buffer: Buffer.from(pngBytes),
      });
      const [upload] = await Promise.all([
        page.waitForResponse((candidate) => candidate.request().method() === "POST" && candidate.url().endsWith("/api/modules/files/upload")),
        page.getByRole("button", { name: procedureItemsText.send, exact: true }).click(),
      ]);
      expect(upload.status()).toBe(200);
      await expect(page.getByRole("button", { name: `${procedureItemsText.preview} scan.png` })).toBeVisible({ timeout: 30_000 });
      await capture(page, "upload-accepted");
    } finally {
      await context.close();
    }
  });
});

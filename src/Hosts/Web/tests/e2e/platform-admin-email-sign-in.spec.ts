import { existsSync, readFileSync, statSync } from "node:fs";
import { expect, test, type Browser, type Page } from "@playwright/test";
import { Routes as PlatformRoutes } from "../../apps/platform-admin/src/app/routes";
import { getMessages } from "../../apps/platform-admin/src/localization/messages";
import {
  authenticationLoginEmailBegin,
  authenticationLoginEmailComplete,
  authenticationSessionGet,
} from "../../shared/auth/src/api/generated/authentication/authentication";
import { localeCookieName } from "../../shared/localization/src/localeCookie";

const platformOrigin = process.env.PLAYWRIGHT_PLATFORM_ADMIN_ORIGIN ?? "http://localhost:3006";
const smtpCapturePath = process.env.VITARISH_E2E_SMTP_CAPTURE_PATH;
// E2eFixtureSeeder.EmailSignInPlatformAdminEmail: a platform admin no other spec signs in.
const platformAdminEmail = "e2e-email-sign-in-platform-admin@example.test";
const messages = getMessages("en");

function generatedPath(operation: { toString(): string }): string {
  const match = /url:\s*[`"']([^`"']+)[`"']/.exec(operation.toString());
  if (!match) throw new Error("The generated authentication client no longer exposes this operation's URL.");
  return match[1];
}

async function readDeliveredCode(previousModifiedAt: number): Promise<string> {
  await expect.poll(() => {
    return Boolean(smtpCapturePath && existsSync(smtpCapturePath) && statSync(smtpCapturePath).mtimeMs > previousModifiedAt);
  }).toBe(true);

  const message = readFileSync(smtpCapturePath!, "utf8");
  const code = /Your verification code is:\s*(?:<strong>)?([^<\s]+)(?:<\/strong>)?/i.exec(message)?.[1];
  expect(code).toEqual(expect.any(String));
  return code!;
}

async function signInWithEmailCode(browser: Browser, clientIp: string): Promise<Page> {
  const context = await browser.newContext({ extraHTTPHeaders: { "X-Forwarded-For": clientIp } });
  await context.addCookies([{ name: localeCookieName, value: "en", url: platformOrigin }]);
  const page = await context.newPage();
  await page.goto(`${platformOrigin}${PlatformRoutes.Audit}`, { waitUntil: "domcontentloaded" });
  await expect(page).toHaveURL(`${platformOrigin}${PlatformRoutes.Login}`, { timeout: 30_000 });

  await page.getByRole("link", { name: new RegExp(messages.auth.login.emailMethod) }).click();
  await expect(page).toHaveURL(`${platformOrigin}${PlatformRoutes.LoginEmail}`);
  await page.getByLabel(messages.auth.otp.emailLabel, { exact: true }).fill(platformAdminEmail);
  const previousModifiedAt = existsSync(smtpCapturePath!) ? statSync(smtpCapturePath!).mtimeMs : 0;
  const [beginResponse] = await Promise.all([
    page.waitForResponse((response) => response.url().endsWith(generatedPath(authenticationLoginEmailBegin))),
    page.getByRole("button", { name: messages.auth.otp.sendAction, exact: true }).click(),
  ]);
  expect(beginResponse.status()).toBe(200);

  await page.getByLabel(messages.auth.otp.codeLabel).fill(await readDeliveredCode(previousModifiedAt));
  const [completeResponse] = await Promise.all([
    page.waitForResponse((response) => response.url().endsWith(generatedPath(authenticationLoginEmailComplete))),
    page.getByRole("button", { name: messages.auth.otp.verifyAction, exact: true }).click(),
  ]);
  expect(completeResponse.status()).toBe(200);
  return page;
}

// A platform admin without a password reaches the admin app through the email-code
// method: signed-out visitors land on the method picker, and the verified code opens
// an authorized session on the admin host. Each test uses its own client IP so the
// begin rate-limit partition is shared with no earlier scenario.
test("a platform admin signs in to the admin app with an email code", async ({ browser }) => {
  expect(smtpCapturePath, "VITARISH_E2E_SMTP_CAPTURE_PATH must be supplied by the two-host runner").toBeTruthy();
  const page = await signInWithEmailCode(browser, "198.51.100.96");
  try {
    await expect(page).toHaveURL(`${platformOrigin}${PlatformRoutes.Audit}`, { timeout: 30_000 });
    await expect(page.getByText(messages.admin.forbiddenTitle)).toHaveCount(0);
    const session = await page.request.get(`${platformOrigin}${generatedPath(authenticationSessionGet)}`);
    expect(session.status()).toBe(200);
  } finally {
    await page.context().close();
  }
});

// A practice is set up entirely in the admin app: the new owner is chosen by name
// when creating the tenant, and the tenant opens from the directory to take its host.
test("a platform admin sets up a practice tenant through the admin app", async ({ browser }) => {
  expect(smtpCapturePath, "VITARISH_E2E_SMTP_CAPTURE_PATH must be supplied by the two-host runner").toBeTruthy();
  const page = await signInWithEmailCode(browser, "198.51.100.97");
  const suffix = Date.now().toString(36);
  const ownerName = `E2E Setup Owner ${suffix}`;
  const tenantName = `E2E Setup Clinic ${suffix}`;
  const hostname = `setup-${suffix}.clinic.e2e.example.test`;
  try {
    await page.goto(`${platformOrigin}${PlatformRoutes.Owners.New}`, { waitUntil: "domcontentloaded" });
    await page.getByLabel(messages.admin.owners.name).fill(ownerName);
    await page.getByRole("button", { name: messages.admin.owners.create }).click();
    await expect(page).toHaveURL(`${platformOrigin}${PlatformRoutes.Owners.Index}`);
    await expect(page.getByRole("link", { name: ownerName })).toBeVisible({ timeout: 30_000 });

    await page.goto(`${platformOrigin}${PlatformRoutes.Tenants.New}`, { waitUntil: "domcontentloaded" });
    await page.getByLabel(messages.admin.tenants.name).fill(tenantName);
    await page.getByLabel(messages.admin.tenants.appId).fill(`e2e-setup-${suffix}`);
    await expect(page.getByRole("option", { name: ownerName })).toBeAttached({ timeout: 30_000 });
    await page.getByLabel(messages.admin.tenants.owner).selectOption({ label: ownerName });
    await page.getByRole("button", { name: messages.admin.tenants.create }).click();
    await expect(page).toHaveURL(`${platformOrigin}${PlatformRoutes.Tenants.Index}`);

    await page.getByRole("link", { name: tenantName }).click();
    await expect(page).toHaveURL(new RegExp(`${platformOrigin}/tenants/[0-9a-f-]{36}$`), { timeout: 30_000 });
    await page.getByLabel(messages.admin.tenants.indicatorValue).fill(hostname);
    await page.getByRole("button", { name: messages.admin.tenants.createIndicator }).click();
    await expect(page.getByText(hostname)).toBeVisible({ timeout: 30_000 });
  } finally {
    await page.context().close();
  }
});

import { expect, test, type Browser, type BrowserContext, type Page } from "@playwright/test";
import { existsSync, readFileSync, statSync } from "node:fs";
import { resolve } from "node:path";
import { Routes } from "../../apps/private-practice-owner/src/app/routes";
import { catalogs } from "../../apps/private-practice-owner/src/localization/messages";
import {
  twoHostE2eClinicOrigin,
  twoHostE2eForwardedForHeaderName,
  twoHostE2eLandingOrigin,
  twoHostE2eLocaleEnvironmentKey,
  twoHostE2ePasswordEnvironmentKey,
  twoHostE2ePasswordlessPhoneClientIp,
  twoHostE2eSignInOrigin,
  twoHostE2eSmsCapturePathEnvironmentKey,
} from "../../scripts/two-host-e2e-artifacts.mjs";
import {
  authenticationLoginPassword,
  authenticationLoginPasswordOtpComplete,
  authenticationLoginPhoneBegin,
  authenticationLoginPhoneComplete,
  authenticationLogoutPost,
  getAuthenticationAntiforgeryGetQueryKey,
  getAuthenticationSessionGetQueryKey,
} from "../../shared/auth/src/api/generated/authentication/authentication";
import { defaultLocale, toSupportedLocale } from "../../shared/localization/src/locales";
import { localeCookieName } from "../../shared/localization/src/localeCookie";
import { REQUEST_ID_HEADER, TENANT_HOST_HEADER } from "../../shared/api-client/src/apiFetch";
import { continueToClinic } from "./signInHost";
import { generatedOperationPath } from "./generatedOperationPath";

function sourceString(path: string, name: string): string {
  const source = readFileSync(resolve(process.cwd(), path), "utf8");
  const value = new RegExp(`public const string ${name} = "([^"]+)";`).exec(source)?.[1];
  if (!value) throw new Error(`Source constant ${name} was not found in ${path}.`);
  return value;
}

const fixtureSource = "../E2e/Infrastructure/Fixtures/E2eFixtureSeeder.cs";
const passwordlessPhone = sourceString(fixtureSource, "PasswordlessPhoneAccountPhoneNumber");
const passwordlessUserName = sourceString(fixtureSource, "PasswordlessPhoneAccountUserName");
const locale = toSupportedLocale(process.env[twoHostE2eLocaleEnvironmentKey]) ?? defaultLocale;
const messages = catalogs[locale];
const authText = messages.auth;
const accountText = messages.account;
const password = process.env[twoHostE2ePasswordEnvironmentKey];
const smsCapturePath = process.env[twoHostE2eSmsCapturePathEnvironmentKey];
const authenticationSessionPath = getAuthenticationSessionGetQueryKey()[0];
const antiforgeryPath = getAuthenticationAntiforgeryGetQueryKey()[0];
const fieldName = (label: string) => new RegExp(`^${label.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")}\\*?$`);

const modifiedAt = (path: string) => existsSync(path) ? statSync(path).mtimeMs : 0;

async function changedFile(path: string, previousModifiedAt: number): Promise<string> {
  await expect.poll(() => existsSync(path) && statSync(path).mtimeMs > previousModifiedAt).toBe(true);
  return readFileSync(path, "utf8");
}

async function deliveredSmsCode(previousModifiedAt: number, recipientDigits: string): Promise<string> {
  const delivered = (await changedFile(smsCapturePath!, previousModifiedAt))
    .trim()
    .split(/\r?\n/)
    .map((line) => JSON.parse(line) as { Recipient: string; Text: string });
  const code = [...delivered].reverse().find((message) => message.Recipient.endsWith(recipientDigits))?.Text;
  expect(code).toEqual(expect.stringMatching(/^\d+$/));
  return code!;
}

async function newAccountContext(browser: Browser): Promise<{ context: BrowserContext; page: Page }> {
  const context = await browser.newContext({
    extraHTTPHeaders: { [twoHostE2eForwardedForHeaderName]: twoHostE2ePasswordlessPhoneClientIp },
  });
  await context.addCookies(
    [twoHostE2eSignInOrigin, twoHostE2eLandingOrigin, twoHostE2eClinicOrigin]
      .map((url) => ({ name: localeCookieName, value: locale, url })),
  );
  return { context, page: await context.newPage() };
}

async function getSessionStatus(page: Page): Promise<number> {
  return page.evaluate(async ({ tenantHeader, path }) =>
    (await fetch(path, { credentials: "include", headers: { [tenantHeader]: location.host } })).status,
  { tenantHeader: TENANT_HOST_HEADER, path: authenticationSessionPath });
}

async function signOut(page: Page): Promise<void> {
  const status = await page.evaluate(async ({ path, tokenPath, tenantHeader, requestIdHeader }) => {
    const headers = { [tenantHeader]: location.host };
    const token = await fetch(tokenPath, { credentials: "include", headers }).then((response) => response.json()) as {
      headerName: string;
      requestToken: string;
    };
    const response = await fetch(path, {
      method: "POST",
      credentials: "include",
      headers: {
        ...headers,
        "Content-Type": "application/json",
        [requestIdHeader]: crypto.randomUUID(),
        [token.headerName]: token.requestToken,
      },
      body: JSON.stringify({ logoutId: null, returnUrl: null }),
    });
    return response.status;
  }, {
    path: generatedOperationPath(authenticationLogoutPost),
    tokenPath: antiforgeryPath,
    tenantHeader: TENANT_HOST_HEADER,
    requestIdHeader: REQUEST_ID_HEADER,
  });
  expect(status).toBe(200);
}

test(`a passwordless phone account can set a password and enable two-step sign-in ${locale}`, async ({ browser }, testInfo) => {
  test.setTimeout(300_000);
  expect(password, "The disposable host supplies the password to set.").toBeTruthy();
  expect(smsCapturePath, "The two-host runner supplies the SMS capture path.").toBeTruthy();

  const { context, page } = await newAccountContext(browser);
  try {
    await page.goto(`${twoHostE2eSignInOrigin}${Routes.Login.Phone}`, { waitUntil: "domcontentloaded" });
    await page.getByRole("textbox", { name: fieldName(authText.otp.phoneLabel) }).fill(passwordlessPhone);
    const firstCodePrevious = modifiedAt(smsCapturePath!);
    const [phoneBegin] = await Promise.all([
      page.waitForResponse((response) => response.url().endsWith(generatedOperationPath(authenticationLoginPhoneBegin))),
      page.getByRole("button", { name: authText.otp.sendAction, exact: true }).click(),
    ]);
    expect(phoneBegin.status()).toBe(200);
    const phoneCode = await deliveredSmsCode(firstCodePrevious, passwordlessPhone.slice(-7));
    await page.getByLabel(authText.otp.codeLabel).fill(phoneCode);
    const [phoneComplete] = await Promise.all([
      page.waitForResponse((response) => response.url().endsWith(generatedOperationPath(authenticationLoginPhoneComplete))),
      page.getByRole("button", { name: authText.otp.verifyAction, exact: true }).click(),
    ]);
    expect(phoneComplete.status()).toBe(200);
    await continueToClinic(page);

    await page.goto(`${twoHostE2eClinicOrigin}${Routes.Account.Password}`, { waitUntil: "domcontentloaded" });
    await expect.poll(() => getSessionStatus(page)).toBe(200);
    await expect(page.getByLabel(accountText.password.currentPassword, { exact: true })).toHaveCount(0);
    await page.screenshot({ path: testInfo.outputPath("passwordless-account-password.png"), fullPage: true });

    await page.getByLabel(accountText.password.newPassword, { exact: true }).fill(password!);
    await page.getByLabel(accountText.password.confirmNewPassword, { exact: true }).fill(password!);
    await page.getByRole("button", { name: accountText.password.setFirstAction, exact: true }).click();
    await expect(page.getByText(accountText.password.firstSetSucceeded, { exact: true })).toBeVisible();

    const twoStepToggle = page.getByRole("switch", { name: accountText.signInSecurity.toggleLabel, exact: true });
    const securityRegion = page.getByRole("region", { name: accountText.signInSecurity.title, exact: true });
    await expect(twoStepToggle).not.toBeChecked();
    await expect(page.getByText(accountText.signInSecurity.disabled, { exact: true })).toBeVisible();
    await expect(securityRegion).toContainText(accountText.signInSecurity.channelPhone);
    // The HeroUI switch keeps its native input visually hidden; operate it the way a keyboard user does.
    await twoStepToggle.focus();
    await page.keyboard.press("Space");
    await expect(twoStepToggle).toBeChecked();
    await expect(page.getByText(accountText.signInSecurity.enabled, { exact: true })).toBeVisible();
    await expect(page.getByText(accountText.signInSecurity.updated, { exact: true })).toBeVisible();
    await page.screenshot({ path: testInfo.outputPath("two-step-phone-enabled.png"), fullPage: true });

    await signOut(page);
    await expect.poll(() => getSessionStatus(page)).toBe(401);
    await page.goto(`${twoHostE2eSignInOrigin}${Routes.Login.Password}`, { waitUntil: "domcontentloaded" });
    await page.locator('input[name="username"]').fill(passwordlessUserName);
    await page.locator('input[name="password"]').fill(password!);
    const secondCodePrevious = modifiedAt(smsCapturePath!);
    const [passwordChallenge] = await Promise.all([
      page.waitForResponse((response) => response.url().endsWith(generatedOperationPath(authenticationLoginPassword))),
      page.locator('button[type="submit"]').click(),
    ]);
    expect(passwordChallenge.ok()).toBe(false);
    const challenge = await passwordChallenge.json() as {
      challengeId: string;
      usesEmailSecondFactor: boolean;
      maskedDestination: string;
      challengeExpiresAt: string;
      remainingAttempts: number;
    };
    expect(challenge.challengeId).toEqual(expect.any(String));
    expect(challenge.usesEmailSecondFactor).toBe(false);
    expect(challenge.challengeExpiresAt).toEqual(expect.any(String));
    expect(challenge.remainingAttempts).toBeGreaterThan(0);
    expect(challenge.maskedDestination).not.toBe(passwordlessPhone);
    expect(challenge.maskedDestination).not.toContain("@");
    await page.screenshot({ path: testInfo.outputPath("password-phone-otp-step.png"), fullPage: true });

    const secondCode = await deliveredSmsCode(secondCodePrevious, passwordlessPhone.slice(-7));
    await page.getByLabel(authText.login.otpLabel).fill(secondCode);
    const [otpComplete] = await Promise.all([
      page.waitForResponse((response) => response.url().endsWith(generatedOperationPath(authenticationLoginPasswordOtpComplete))),
      page.getByRole("button", { name: authText.login.otpVerifyAction, exact: true }).click(),
    ]);
    expect(otpComplete.status()).toBe(200);
    await continueToClinic(page);
    await expect(page).toHaveURL(`${twoHostE2eClinicOrigin}${Routes.Clinic.Index}`);
    await expect.poll(() => getSessionStatus(page)).toBe(200);
    await page.screenshot({ path: testInfo.outputPath("password-and-second-step-destination.png"), fullPage: true });
  } finally {
    if (await getSessionStatus(page).catch(() => 401) === 200)
      await signOut(page);
    await context.close();
  }
});

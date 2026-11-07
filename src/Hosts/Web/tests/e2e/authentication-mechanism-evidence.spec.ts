import { expect, test, type Browser, type BrowserContext, type CDPSession, type Page, type TestInfo } from "@playwright/test";
import { randomInt } from "node:crypto";
import { existsSync, readFileSync, statSync } from "node:fs";
import { resolve } from "node:path";
import { Routes as ClinicRoutes } from "../../apps/private-practice-owner/src/app/routes";
import { enMessages } from "../../apps/private-practice-owner/src/localization/messages/en";
import { faMessages } from "../../apps/private-practice-owner/src/localization/messages/fa";
import { Routes as PlatformRoutes } from "../../apps/platform-admin/src/app/routes";
import { authMessageCatalogs } from "../../shared/auth/src/localization";
import { twoHostE2eClinicOrigin, twoHostE2eForwardedForHeaderName as forwardedForHeaderName, twoHostE2eLocaleEnvironmentKey, twoHostE2ePasswordEnvironmentKey, twoHostE2ePlatformAdminOrigin, twoHostE2eSignInOrigin, twoHostE2eSmsCapturePathEnvironmentKey } from "../../scripts/two-host-e2e-artifacts.mjs";
import { REQUEST_ID_HEADER, TENANT_HOST_HEADER } from "../../shared/api-client/src/apiFetch";
import {
  authenticationLoginEmailBegin,
  authenticationLoginEmailComplete,
  authenticationLoginPasskeyBegin,
  authenticationLoginPasskeyComplete,
  authenticationLoginPassword,
  authenticationLoginPasswordOtpComplete,
  authenticationLoginPasswordOtpResend,
  authenticationLoginPhoneBegin,
  authenticationLoginPhoneComplete,
  authenticationLogoutPost,
  getAuthenticationAntiforgeryGetQueryKey,
} from "../../shared/auth/src/api/generated/authentication/authentication";
import { localeCookieName } from "../../shared/localization/src/localeCookie";
import { getAuditPlatformSecurityEventsListQueryKey } from "../../shared/security/src/api/generated/audit/audit";
import type { AuditPlatformSecurityEventsListParams, SecurityAuditEventReadDto } from "../../shared/security/src/api/generated/audit/model";
import { auditSortFields } from "../../shared/security/src/api/securityAuditSortFields";
import { localizePlatformAuditEvent, securityMessageCatalogs } from "../../shared/security/src/localization";
import { auditDetailEvidenceViewport } from "./auditEvidenceViewport";
import { generatedOperationPath } from "./generatedOperationPath";
import { signInToClinic } from "./signInHost";

// Read fixture and event identifiers from their source owners, never copy their values here.
function sourceString(path: string, name: string): string {
  const source = readFileSync(resolve(process.cwd(), path), "utf8");
  const value = new RegExp(`public const string ${name} = "([^"]+)";`).exec(source)?.[1];
  if (!value) throw new Error(`Source constant ${name} was not found in ${path}.`);
  return value;
}
const fixtureSource = "../E2e/Infrastructure/Fixtures/E2eFixtureSeeder.cs";
const auditSource = "../../Libraries/Audit/Audit.Domain/SecurityAuditEventCatalog.cs";
const reviewerUserName = sourceString(fixtureSource, "UserManagementPlatformAdminUserName");
const emailSignInAddress = sourceString(fixtureSource, "EmailSignInPlatformAdminEmail");
const twoFactorUserName = sourceString(fixtureSource, "TwoFactorPlatformAdminUserName");
const twoFactorPhoneNumber = sourceString(fixtureSource, "TwoFactorPlatformAdminPhoneNumber");
const emailTwoFactorUserName = sourceString(fixtureSource, "EmailTwoFactorPlatformAdminUserName");
const emailTwoFactorAddress = sourceString(fixtureSource, "EmailTwoFactorPlatformAdminEmail");
const passkeyUserName = sourceString(fixtureSource, "PasskeyUserName");

const locale = process.env[twoHostE2eLocaleEnvironmentKey] === "fa" ? "fa" : "en";
const clinicText = locale === "fa" ? faMessages : enMessages;
const authText = clinicText.auth;
const auditText = securityMessageCatalogs[locale].security.platformAudit;
const password = process.env[twoHostE2ePasswordEnvironmentKey];
const smsCapturePath = process.env[twoHostE2eSmsCapturePathEnvironmentKey];
const smtpCapturePath = process.env.VITARISH_E2E_SMTP_CAPTURE_PATH;
const platformOrigin = twoHostE2ePlatformAdminOrigin;

// Each method has its own client address, which also identifies its record below.
const clientIps = { password: "198.51.100.242", email: "198.51.100.243", phone: "198.51.100.244", passwordOtp: "198.51.100.245", passkey: "198.51.100.246", emailPasswordOtp: "198.51.100.247" } as const;

// A platform authenticator that keeps discoverable credentials and verifies the user without a prompt.
const virtualAuthenticator = {
  protocol: "ctap2",
  ctap2Version: "ctap2_1",
  transport: "internal",
  hasResidentKey: true,
  hasUserVerification: true,
  automaticPresenceSimulation: true,
  isUserVerified: true,
} as const;
const storedCredentials = (cdp: CDPSession, authenticatorId: string) => cdp.send("WebAuthn.getCredentials", { authenticatorId });
type VirtualCredential = Awaited<ReturnType<typeof storedCredentials>>["credentials"][number];

// A missing control fails within a minute instead of waiting out the whole test.
test.use({ actionTimeout: 60_000 });

const fieldName = (label: string) => new RegExp(`^${label.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")}\\*?$`);

async function changedFile(path: string, previousModifiedAt: number): Promise<string> {
  await expect.poll(() => existsSync(path) && statSync(path).mtimeMs > previousModifiedAt).toBe(true);
  return readFileSync(path, "utf8");
}

const modifiedAt = (path: string) => existsSync(path) ? statSync(path).mtimeMs : 0;

async function newClient(browser: Browser, clientIp: string, cookieOrigin: string): Promise<{ context: BrowserContext; page: Page }> {
  const context = await browser.newContext({ extraHTTPHeaders: { [forwardedForHeaderName]: clientIp } });
  await context.addCookies([{ name: localeCookieName, value: locale, url: cookieOrigin }]);
  return { context, page: await context.newPage() };
}

async function signOut(page: Page): Promise<void> {
  const status = await page.evaluate(async ({ path, tokenPath, tenantHeader, requestIdHeader }) => {
    const headers = { [tenantHeader]: location.host };
    const token = await fetch(tokenPath, { credentials: "include", headers }).then(response => response.json()) as { headerName: string; requestToken: string };
    const response = await fetch(path, { method: "POST", credentials: "include", headers: {
      ...headers, "Content-Type": "application/json", [requestIdHeader]: crypto.randomUUID(), [token.headerName]: token.requestToken,
    }, body: JSON.stringify({ logoutId: null, returnUrl: null }) });
    return response.status;
  }, { path: generatedOperationPath(authenticationLogoutPost), tokenPath: getAuthenticationAntiforgeryGetQueryKey()[0], tenantHeader: TENANT_HOST_HEADER, requestIdHeader: REQUEST_ID_HEADER });
  expect(status).toBe(200);
}

async function signInWithEmailCode(page: Page): Promise<void> {
  await page.goto(`${platformOrigin}${PlatformRoutes.LoginEmail}`, { waitUntil: "domcontentloaded" });
  await page.getByLabel(authText.otp.emailLabel, { exact: true }).fill(emailSignInAddress);
  const previous = modifiedAt(smtpCapturePath!);
  const [begin] = await Promise.all([
    page.waitForResponse(response => response.url().endsWith(generatedOperationPath(authenticationLoginEmailBegin))),
    page.getByRole("button", { name: authText.otp.sendAction, exact: true }).click(),
  ]);
  expect(begin.status()).toBe(200);
  const code = /Your verification code is:\s*(?:<strong>)?([^<\s]+)(?:<\/strong>)?/i.exec(await changedFile(smtpCapturePath!, previous))?.[1];
  expect(code).toEqual(expect.any(String));
  await page.getByLabel(authText.otp.codeLabel).fill(code!);
  const [complete] = await Promise.all([
    page.waitForResponse(response => response.url().endsWith(generatedOperationPath(authenticationLoginEmailComplete))),
    page.getByRole("button", { name: authText.otp.verifyAction, exact: true }).click(),
  ]);
  expect(complete.status()).toBe(200);
}

/** The newest text message the capture holds for a number ending in `recipientDigits`. */
async function deliveredSmsCode(previousModifiedAt: number, recipientDigits: string): Promise<string> {
  const delivered = (await changedFile(smsCapturePath!, previousModifiedAt)).trim().split(/\r?\n/).map(line => JSON.parse(line) as { Recipient: string; Text: string });
  const code = [...delivered].reverse().find(message => message.Recipient.endsWith(recipientDigits))?.Text;
  expect(code).toEqual(expect.stringMatching(/^\d+$/));
  return code!;
}

async function deliveredEmailCode(previousModifiedAt: number, recipientEmail: string): Promise<string> {
  const message = await changedFile(smtpCapturePath!, previousModifiedAt);
  expect(message.toLowerCase()).toContain(recipientEmail.toLowerCase());
  const htmlPart = message.split(/\r?\n--/).find(part => /^Content-Type:\s*text\/html\b/im.test(part));
  expect(htmlPart).toBeTruthy();
  const [headers, ...bodyParts] = htmlPart!.split(/\r?\n\r?\n/);
  const body = bodyParts.join("\n\n");
  const html = /Content-Transfer-Encoding:\s*base64/i.test(headers!)
    ? Buffer.from(body.replace(/\s/g, ""), "base64").toString("utf8")
    : body.replace(/=\r?\n/g, "").replace(/=([0-9a-f]{2})/gi, (_, hex: string) => String.fromCharCode(parseInt(hex, 16)));
  const code = /<strong>\s*(\d{6})\s*<\/strong>/i.exec(html)?.[1];
  expect(code).toEqual(expect.stringMatching(/^\d+$/));
  return code!;
}

async function signInWithPasswordAndPhoneCode(page: Page): Promise<void> {
  await page.goto(`${platformOrigin}${PlatformRoutes.LoginPassword}`, { waitUntil: "domcontentloaded" });
  await page.locator('input[name="username"]').fill(twoFactorUserName);
  await page.locator('input[name="password"]').fill(password!);
  const previous = modifiedAt(smsCapturePath!);
  const [passwordStep] = await Promise.all([
    page.waitForResponse(response => response.url().endsWith(generatedOperationPath(authenticationLoginPassword)) && response.request().method() === "POST"),
    page.locator('button[type="submit"]').click(),
  ]);
  // A correct password alone does not sign this account in; it asks for the code sent to the phone.
  expect(passwordStep.ok()).toBe(false);
  const code = await deliveredSmsCode(previous, twoFactorPhoneNumber.slice(-10));
  await page.getByLabel(fieldName(authText.login.otpLabel)).fill(code);
  const [complete] = await Promise.all([
    page.waitForResponse(response => response.url().endsWith(generatedOperationPath(authenticationLoginPasswordOtpComplete))),
    page.getByRole("button", { name: authText.login.otpVerifyAction, exact: true }).click(),
  ]);
  expect(complete.status()).toBe(200);
}

async function signInWithPasswordAndEmailCode(page: Page, testInfo: TestInfo): Promise<void> {
  await page.goto(`${platformOrigin}${PlatformRoutes.LoginPassword}`, { waitUntil: "domcontentloaded" });
  await page.locator('input[name="username"]').fill(emailTwoFactorUserName);
  await page.locator('input[name="password"]').fill(password!);
  const previous = modifiedAt(smtpCapturePath!);
  const [passwordStep] = await Promise.all([
    page.waitForResponse(response => response.url().endsWith(generatedOperationPath(authenticationLoginPassword)) && response.request().method() === "POST"),
    page.locator('button[type="submit"]').click(),
  ]);
  expect(passwordStep.ok()).toBe(false);
  const challenge = await passwordStep.json() as {
    challengeId: string;
    usesEmailSecondFactor: boolean;
    maskedDestination: string;
    challengeExpiresAt: string;
    remainingAttempts: number;
  };
  expect(challenge.challengeId).toEqual(expect.any(String));
  expect(challenge.usesEmailSecondFactor).toBe(true);
  expect(challenge.challengeExpiresAt).toEqual(expect.any(String));
  expect(challenge.remainingAttempts).toBeGreaterThan(0);
  expect(challenge.maskedDestination).toContain("@");
  expect(challenge.maskedDestination).not.toBe(emailTwoFactorAddress);
  await expect(page.getByText(authMessageCatalogs[locale].auth.passwordLoginOtp.emailStepDescription, { exact: true })).toBeVisible();
  await expect(page.getByText(challenge.maskedDestination, { exact: true })).toBeVisible();
  await expect(page.getByText(emailTwoFactorAddress, { exact: true })).toHaveCount(0);
  await page.screenshot({ path: testInfo.outputPath("password-email-otp-step.png"), fullPage: true });

  const firstCode = await deliveredEmailCode(previous, emailTwoFactorAddress);
  const codeField = page.getByLabel(fieldName(authText.login.otpLabel));
  await codeField.fill(firstCode);

  const resendPrevious = modifiedAt(smtpCapturePath!);
  const [resend] = await Promise.all([
    page.waitForResponse(response => response.url().endsWith(generatedOperationPath(authenticationLoginPasswordOtpResend))),
    page.getByRole("button", { name: authText.otp.resendAction, exact: true }).click(),
  ]);
  expect(resend.status()).toBe(200);
  await expect(page.getByText(challenge.maskedDestination, { exact: true })).toBeVisible();
  const code = await deliveredEmailCode(resendPrevious, emailTwoFactorAddress);
  await codeField.fill(code);
  const [complete] = await Promise.all([
    page.waitForResponse(response => response.url().endsWith(generatedOperationPath(authenticationLoginPasswordOtpComplete))),
    page.getByRole("button", { name: authText.login.otpVerifyAction, exact: true }).click(),
  ]);
  expect(complete.status()).toBe(200);
}

/** Registers a passkey for the passkey fixture user through the account page and returns the stored credential. */
async function registerPasskey(browser: Browser): Promise<VirtualCredential> {
  const registration = await newClient(browser, clientIps.passkey, twoHostE2eClinicOrigin);
  try {
    const cdp = await registration.context.newCDPSession(registration.page);
    await cdp.send("WebAuthn.enable");
    const { authenticatorId } = await cdp.send("WebAuthn.addVirtualAuthenticator", { options: virtualAuthenticator });
    await signInToClinic(registration.page, passkeyUserName, password!);
    await registration.page.goto(`${twoHostE2eClinicOrigin}${ClinicRoutes.Account.Passkeys}`, { waitUntil: "domcontentloaded" });
    await registration.page.getByRole("button", { name: clinicText.account.passkeys.registerAction }).click();
    await expect(registration.page.getByText(clinicText.account.passkeys.registered)).toBeVisible();
    const { credentials } = await storedCredentials(cdp, authenticatorId);
    expect(credentials).toHaveLength(1);
    await signOut(registration.page);
    return credentials[0]!;
  } finally {
    await registration.context.close();
  }
}

/**
 * Signs in with the registered passkey through the public passkey sign-in endpoints and the browser's own
 * WebAuthn ceremony. Locally the passkey belongs to "localhost", which browsers treat as a top-level name,
 * so the "auth.localhost" sign-in page cannot use it; the ceremony runs on the clinic origin instead and
 * sends the same requests that page sends.
 */
async function signInWithPasskey(page: Page, credential: VirtualCredential): Promise<void> {
  const cdp = await page.context().newCDPSession(page);
  await cdp.send("WebAuthn.enable");
  const { authenticatorId } = await cdp.send("WebAuthn.addVirtualAuthenticator", { options: virtualAuthenticator });
  await cdp.send("WebAuthn.addCredential", { authenticatorId, credential });
  const tokenPath = getAuthenticationAntiforgeryGetQueryKey()[0];
  await page.goto(`${twoHostE2eClinicOrigin}${tokenPath}`, { waitUntil: "domcontentloaded" });
  const status = await page.evaluate(async ({ beginPath, completePath, tokenPath, tenantHeader, requestIdHeader }) => {
    const toBuffer = (value: string) => {
      const base64 = value.replace(/-/g, "+").replace(/_/g, "/");
      return Uint8Array.from(atob(base64 + "=".repeat((4 - (base64.length % 4)) % 4)), character => character.charCodeAt(0));
    };
    const toBase64Url = (buffer: ArrayBuffer) =>
      btoa(String.fromCharCode(...new Uint8Array(buffer))).replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
    const headers = { [tenantHeader]: location.host };
    const token = await fetch(tokenPath, { credentials: "include", headers }).then(response => response.json()) as { headerName: string; requestToken: string };
    const post = (path: string, body: unknown) => fetch(path, { method: "POST", credentials: "include", headers: {
      ...headers, "Content-Type": "application/json", [requestIdHeader]: crypto.randomUUID(), [token.headerName]: token.requestToken,
    }, body: JSON.stringify(body) });
    const challenge = await (await post(beginPath, { userNameOrEmail: null, returnUrl: null })).json() as { challengeId: string; optionsJson: string };
    const options = JSON.parse(challenge.optionsJson) as { challenge: string; rpId: string; timeout: number; userVerification: UserVerificationRequirement };
    const assertion = await navigator.credentials.get({ publicKey: {
      challenge: toBuffer(options.challenge), rpId: options.rpId, timeout: options.timeout, userVerification: options.userVerification, allowCredentials: [],
    } }) as PublicKeyCredential;
    const response = assertion.response as AuthenticatorAssertionResponse;
    const credentialJson = JSON.stringify({
      id: assertion.id,
      rawId: toBase64Url(assertion.rawId),
      type: assertion.type,
      response: {
        authenticatorData: toBase64Url(response.authenticatorData),
        clientDataJSON: toBase64Url(response.clientDataJSON),
        signature: toBase64Url(response.signature),
        userHandle: response.userHandle ? toBase64Url(response.userHandle) : null,
      },
    });
    return (await post(completePath, { challengeId: challenge.challengeId, credentialJson, returnUrl: null })).status;
  }, {
    beginPath: generatedOperationPath(authenticationLoginPasskeyBegin),
    completePath: generatedOperationPath(authenticationLoginPasskeyComplete),
    tokenPath,
    tenantHeader: TENANT_HOST_HEADER,
    requestIdHeader: REQUEST_ID_HEADER,
  });
  expect(status).toBe(200);
}

async function signInWithPhoneCode(page: Page): Promise<void> {
  const nationalDigits = `912${Array.from({ length: 7 }, () => randomInt(0, 10)).join("")}`;
  await page.goto(`${twoHostE2eSignInOrigin}${ClinicRoutes.Login.Phone}`, { waitUntil: "domcontentloaded" });
  await page.getByRole("textbox", { name: fieldName(authText.otp.phoneLabel) }).fill(`0${nationalDigits}`);
  const previous = modifiedAt(smsCapturePath!);
  const [begin] = await Promise.all([
    page.waitForResponse(response => response.url().endsWith(generatedOperationPath(authenticationLoginPhoneBegin))),
    page.getByRole("button", { name: authText.otp.sendAction, exact: true }).click(),
  ]);
  expect(begin.status()).toBe(200);
  const code = await deliveredSmsCode(previous, nationalDigits);
  await page.getByLabel(fieldName(authText.otp.codeLabel)).fill(code);
  const [complete] = await Promise.all([
    page.waitForResponse(response => response.url().endsWith(generatedOperationPath(authenticationLoginPhoneComplete))),
    page.getByRole("button", { name: authText.otp.verifyAction, exact: true }).click(),
  ]);
  expect(complete.status()).toBe(200);
}

async function captureRecord(reviewer: Page, eventName: string, clientIp: string, filename: string, testInfo: TestInfo): Promise<void> {
  const params: AuditPlatformSecurityEventsListParams = {
    page: 1, pageSize: 25, sortBy: auditSortFields.occurredAt, descending: true, uncataloguedOnly: false, eventName, outcome: "succeeded",
  };
  const query = new URLSearchParams(Object.entries(params).map(([key, value]) => [key, String(value)]));
  const response = await reviewer.request.get(`${platformOrigin}${getAuditPlatformSecurityEventsListQueryKey(params)[0]}?${query}`, {
    headers: { [TENANT_HOST_HEADER]: new URL(platformOrigin).host },
  });
  expect(response.status()).toBe(200);
  const { items } = await response.json() as { items: SecurityAuditEventReadDto[] };
  const record = items.find(item => item.sourceIpAddress === clientIp);
  expect(record?.id, `${eventName} must leave a record from ${clientIp}`).toEqual(expect.any(String));
  expect(record!.subjectId, `${eventName} must name the signed-in user`).toEqual(expect.any(String));
  expect(record!.actorId, `${eventName} must name the signed-in user as its actor`).toBe(record!.subjectId);

  await reviewer.goto(`${platformOrigin}${PlatformRoutes.Audit}`, { waitUntil: "domcontentloaded" });
  await reviewer.getByRole("textbox", { name: auditText.eventId, exact: true }).fill(record!.id!);
  await reviewer.getByRole("button", { name: auditText.applyFilters, exact: true }).click();
  await reviewer.getByRole("row").filter({ hasText: localizePlatformAuditEvent(locale, eventName, eventName) }).first().click();
  const detail = reviewer.getByRole("dialog");
  await expect(detail).toHaveAccessibleName(auditText.detailTitle);
  // The account is both the actor and the subject of its own sign-in.
  await expect(detail.getByText(record!.subjectId!, { exact: true })).toHaveCount(2);
  await expect(detail.getByText(clientIp, { exact: true })).toBeInViewport();
  await reviewer.evaluate(() => Promise.all(document.getAnimations()
    .filter(animation => animation.effect?.getComputedTiming().endTime !== Infinity)
    .map(animation => animation.finished)));
  await reviewer.screenshot({ path: testInfo.outputPath(filename) });
}

test(`each sign-in method leaves its own attributable audit record ${locale}`, async ({ browser }, testInfo) => {
  test.setTimeout(300_000);
  expect(password, "The disposable host supplies the fixture password.").toBeTruthy();
  expect(smsCapturePath, "The two-host runner supplies the SMS capture path.").toBeTruthy();
  expect(smtpCapturePath, "The two-host runner supplies the SMTP capture path.").toBeTruthy();

  const reviewer = await newClient(browser, clientIps.password, platformOrigin);
  try {
    await reviewer.page.setViewportSize(auditDetailEvidenceViewport);
    await reviewer.page.goto(`${platformOrigin}${PlatformRoutes.LoginPassword}`);
    await reviewer.page.locator('input[name="username"]').fill(reviewerUserName);
    await reviewer.page.locator('input[name="password"]').fill(password!);
    const [signIn] = await Promise.all([
      reviewer.page.waitForResponse(response => response.url().endsWith(generatedOperationPath(authenticationLoginPassword)) && response.request().method() === "POST"),
      reviewer.page.locator('button[type="submit"]').click(),
    ]);
    expect(signIn.status()).toBe(200);

    const passkey = await registerPasskey(browser);
    const methods = [
      { key: "email", eventName: sourceString(auditSource, "AuthenticationEmailLoginEvent"), cookieOrigin: platformOrigin, signIn: signInWithEmailCode },
      { key: "phone", eventName: sourceString(auditSource, "AuthenticationPhoneLoginEvent"), cookieOrigin: twoHostE2eSignInOrigin, signIn: signInWithPhoneCode },
      { key: "passwordOtp", eventName: sourceString(auditSource, "AuthenticationPasswordLoginOtpEvent"), cookieOrigin: platformOrigin, signIn: signInWithPasswordAndPhoneCode },
      { key: "emailPasswordOtp", eventName: sourceString(auditSource, "AuthenticationPasswordLoginOtpEvent"), cookieOrigin: platformOrigin, signIn: (page: Page) => signInWithPasswordAndEmailCode(page, testInfo) },
      { key: "passkey", eventName: sourceString(auditSource, "AuthenticationPasskeyLoginEvent"), cookieOrigin: twoHostE2eClinicOrigin, signIn: (page: Page) => signInWithPasskey(page, passkey) },
    ] as const;
    for (const method of methods) {
      const client = await newClient(browser, clientIps[method.key], method.cookieOrigin);
      try {
        await method.signIn(client.page);
        await signOut(client.page);
      } finally {
        await client.context.close();
      }
      await captureRecord(reviewer.page, method.eventName, clientIps[method.key], `sign-in-${method.key}-detail.png`, testInfo);
    }
    await captureRecord(reviewer.page, sourceString(auditSource, "AuthenticationPasswordLoginEvent"), clientIps.password, "sign-in-password-detail.png", testInfo);
  } finally {
    await signOut(reviewer.page);
    await reviewer.context.close();
  }
});

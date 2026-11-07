import { expect, test, type BrowserContext, type Page } from "@playwright/test";
import crypto from "node:crypto";
import { existsSync, mkdirSync, readFileSync, statSync } from "node:fs";
import { dirname } from "node:path";
import { Routes } from "../../apps/private-practice-owner/src/app/routes";
import { enMessages } from "../../apps/private-practice-owner/src/localization/messages/en";
import { faMessages } from "../../apps/private-practice-owner/src/localization/messages/fa";
import {
  twoHostE2eApiOriginEnvironmentKey,
  twoHostE2eLocaleEnvironmentKey,
  twoHostE2eLandingOrigin,
  twoHostE2eOwnerUsernameEnvironmentKey,
  twoHostE2ePasswordEnvironmentKey,
  twoHostE2eProofScreenshotEnvironmentKey,
  twoHostE2eSignInOrigin,
  twoHostE2eSmsCapturePathEnvironmentKey,
} from "../../scripts/two-host-e2e-artifacts.mjs";
import { REQUEST_ID_HEADER, TENANT_HOST_HEADER } from "../../shared/api-client/src/apiFetch";
import {
  authenticationAntiforgeryGet,
  authenticationLoginPassword,
  authenticationLoginPhoneBegin,
  authenticationLoginPhoneComplete,
  authenticationLogoutPost,
  authenticationSessionGet,
} from "../../shared/auth/src/api/generated/authentication/authentication";
import { clinicEnMessages } from "../../shared/clinic/src/localization/en";
import { clinicFaMessages } from "../../shared/clinic/src/localization/fa";
import { LandingAppAvailability } from "../../shared/platform/src/api/generated/landing/model";
import { partiesIssueSignInHomeHandoff } from "../../shared/journey/src/api/generated/parties/parties";
import {
  landingIssueOnboardingHandoff,
  landingIssueWorkspaceHandoff,
  landingListWorkspaces,
} from "../../shared/platform/src/api/generated/landing/landing";
import { signInReturnUrlQueryKey } from "../../shared/journey/src/signInHost";
import { localeCookieName } from "../../shared/localization/src/localeCookie";
import type { SupportedLocale } from "../../shared/localization/src/locales";
import { defaultClinicOrigin, forwardedForHeaderName } from "./signInHost";
import { generatedOperationPath as generatedPath } from "./generatedOperationPath";

const locale: SupportedLocale = process.env[twoHostE2eLocaleEnvironmentKey] === "fa" ? "fa" : "en";
const messages = { en: enMessages, fa: faMessages }[locale];
const authMessages = messages.auth;
const journeyMessages = messages.journey;
const clinicMessages = { en: clinicEnMessages, fa: clinicFaMessages }[locale].clinic;
const smsCapturePath = process.env[twoHostE2eSmsCapturePathEnvironmentKey];
const ownerUsername = process.env[twoHostE2eOwnerUsernameEnvironmentKey];
const password = process.env[twoHostE2ePasswordEnvironmentKey];
const signInOrigin = twoHostE2eSignInOrigin;
const landingOrigin = twoHostE2eLandingOrigin;
const landingHost = new URL(landingOrigin).host;
const onboardingHostname = "start.localhost";
// ClinicTenant.AppId in src/Apps/Clinic/Application/ClinicTenant.cs.
const expectedClinicAppId = "private-practice-owner";
const anonymousClientIp = "198.51.100.124";
const emptyHomeClientIp = "198.51.100.125";
const clinicHomeClientIp = "198.51.100.126";
const safeReturnClientIp = "198.51.100.127";
const unsafeReturnClientIp = "198.51.100.128";
const authenticatedLoginClientIp = "198.51.100.129";
const pageCompileTimeoutMilliseconds = 120_000;
const returnIntent = "/studio";
const unsafeReturnIntent = "//evil.example";
const phoneBeginPath = generatedPath(authenticationLoginPhoneBegin);
const phoneCompletePath = generatedPath(authenticationLoginPhoneComplete);
const passwordLoginPath = generatedPath(authenticationLoginPassword);
const sessionPath = generatedPath(authenticationSessionGet);
const antiforgeryPath = generatedPath(authenticationAntiforgeryGet);
const signInHomeHandoffPath = generatedPath(partiesIssueSignInHomeHandoff);

type CapturedSms = { Recipient: string; Text: string };
type ApiResult = { status: number; body: unknown };
type SignInResult = { continueUrl: string; handoffUrl: string; issuingCookieHeader: string };

function fieldName(label: string) {
  return new RegExp(`^${label.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")}\\*?$`);
}

function randomDigits(length: number): string {
  return Array.from({ length }, () => crypto.randomInt(0, 10).toString()).join("");
}

async function readDeliveredSmsCode(previousModifiedAt: number, nationalDigits: string): Promise<string> {
  await expect.poll(() => Boolean(smsCapturePath && existsSync(smsCapturePath) && statSync(smsCapturePath).mtimeMs > previousModifiedAt)).toBe(true);
  const lines = readFileSync(smsCapturePath!, "utf8").trim().split(/\r?\n/).filter(Boolean).map((line) => JSON.parse(line) as CapturedSms);
  const code = [...lines].reverse().find((entry) => entry.Recipient.endsWith(nationalDigits))?.Text;
  expect(code).toEqual(expect.stringMatching(/^\d+$/));
  return code!;
}

async function callApi(page: Page, method: "GET" | "POST", path: string, body?: unknown): Promise<ApiResult> {
  return page.evaluate(
    async ({ tenantHostHeader, requestIdHeader, antiforgeryRequestPath, requestMethod, requestPath, requestBody, requestId }) => {
      const headers: Record<string, string> = { [tenantHostHeader]: window.location.host, [requestIdHeader]: requestId };
      if (requestMethod === "POST") {
        const antiforgeryResponse = await fetch(antiforgeryRequestPath, { credentials: "include", headers });
        const antiforgery = (await antiforgeryResponse.json()) as { headerName: string; requestToken: string };
        headers[antiforgery.headerName] = antiforgery.requestToken;
        headers["Content-Type"] = "application/json";
      }
      const response = await fetch(requestPath, {
        method: requestMethod,
        credentials: "include",
        headers,
        body: requestBody === undefined ? undefined : JSON.stringify(requestBody),
      });
      const text = await response.text();
      return { status: response.status, body: text ? JSON.parse(text) : null };
    },
    {
      tenantHostHeader: TENANT_HOST_HEADER,
      requestIdHeader: REQUEST_ID_HEADER,
      antiforgeryRequestPath: antiforgeryPath,
      requestMethod: method,
      requestPath: path,
      requestBody: body,
      requestId: `e2e-platform-home-${crypto.randomUUID()}`,
    },
  );
}

async function configureContext(context: BrowserContext, additionalOrigins: string[] = []): Promise<void> {
  await context.addCookies(
    [signInOrigin, landingOrigin, ...additionalOrigins].map((url) => ({ name: localeCookieName, value: locale, url })),
  );
}

async function observeSignInNavigation(page: Page): Promise<{
  navigatedUrls: string[];
  issuingCookieHeader: string;
}> {
  const navigatedUrls: string[] = [];
  let issuingCookieHeader = "";
  page.on("framenavigated", (frame) => {
    if (frame !== page.mainFrame()) return;
    navigatedUrls.push(frame.url());
  });
  await page.route(
    (url) => {
      try {
        return new URL(url.href).pathname === signInHomeHandoffPath;
      } catch {
        return false;
      }
    },
    async (route) => {
      issuingCookieHeader = (await route.request().allHeaders()).cookie ?? "";
      await route.continue();
    },
  );
  return { navigatedUrls, get issuingCookieHeader() { return issuingCookieHeader; } };
}

async function completeLandingSignIn(page: Page, navigatedUrls: string[]): Promise<string> {
  await expect.poll(() => navigatedUrls.some((url) => {
    try {
      const parsed = new URL(url);
      return parsed.origin === landingOrigin && parsed.pathname === Routes.Handoff;
    } catch {
      return false;
    }
  }), { timeout: pageCompileTimeoutMilliseconds }).toBe(true);
  return navigatedUrls.find((url) => {
    try {
      const parsed = new URL(url);
      return parsed.origin === landingOrigin && parsed.pathname === Routes.Handoff;
    } catch {
      return false;
    }
  })!;
}

async function signInWithPhone(
  page: Page,
  phoneDigits: string,
  returnTo?: string,
  startAtLoginIndex = false,
): Promise<SignInResult> {
  expect(smsCapturePath, `${twoHostE2eSmsCapturePathEnvironmentKey} must be supplied by the two-host runner`).toBeTruthy();
  const navigation = await observeSignInNavigation(page);
  const loginUrl = new URL(startAtLoginIndex ? Routes.Login.Index : Routes.Login.Phone, signInOrigin);
  if (returnTo !== undefined) loginUrl.searchParams.set(signInReturnUrlQueryKey, returnTo);
  await page.goto(loginUrl.toString(), { waitUntil: "domcontentloaded", timeout: pageCompileTimeoutMilliseconds });
  if (startAtLoginIndex) {
    if (returnTo !== undefined) {
      await expect(page).toHaveURL(loginUrl.toString(), { timeout: pageCompileTimeoutMilliseconds });
    }
    await page.getByRole("link", { name: authMessages.login.phoneMethod }).click();
    await expect(page).toHaveURL(new RegExp(`^${signInOrigin}${Routes.Login.Phone}(?:[/?#]|$)`), { timeout: pageCompileTimeoutMilliseconds });
  }
  await expect(page.getByRole("heading", { name: authMessages.otp.phoneTitle, exact: true })).toBeVisible({ timeout: pageCompileTimeoutMilliseconds });
  await page.getByRole("textbox", { name: fieldName(authMessages.otp.phoneLabel) }).fill(`0${phoneDigits}`);

  const previousSmsModifiedAt = smsCapturePath && existsSync(smsCapturePath) ? statSync(smsCapturePath).mtimeMs : 0;
  const [beginResponse] = await Promise.all([
    page.waitForResponse((response) => response.url().endsWith(phoneBeginPath)),
    page.getByRole("button", { name: authMessages.otp.sendAction, exact: true }).click(),
  ]);
  expect(beginResponse.status()).toBe(200);

  await page.getByLabel(fieldName(authMessages.otp.codeLabel)).fill(await readDeliveredSmsCode(previousSmsModifiedAt, phoneDigits));
  const [completeResponse] = await Promise.all([
    page.waitForResponse((response) => response.url().endsWith(phoneCompletePath)),
    page.getByRole("button", { name: authMessages.otp.verifyAction, exact: true }).click(),
  ]);
  expect(completeResponse.status()).toBe(200);
  await expect(page).toHaveURL(new RegExp(`${Routes.Account.ProfileComplete}(?:[/?#]|$)`), { timeout: pageCompileTimeoutMilliseconds });
  await expect(page.getByRole("heading", { name: journeyMessages.gates.profile.title })).toBeVisible({ timeout: pageCompileTimeoutMilliseconds });
  await page.getByRole("textbox", { name: fieldName(journeyMessages.gates.profile.firstName) }).fill(`Platform ${phoneDigits.slice(-4)}`);
  await page.getByRole("textbox", { name: fieldName(journeyMessages.gates.profile.lastName) }).fill("Visitor");
  const saveProfileButton = page.getByRole("button", { name: journeyMessages.gates.profile.saveAndContinue, exact: true });
  await expect(saveProfileButton).toBeEnabled({ timeout: pageCompileTimeoutMilliseconds });
  await saveProfileButton.click();
  const continueUrl = await expect.poll(() => navigation.navigatedUrls.find((url) => {
    try {
      const parsed = new URL(url);
      return parsed.origin === signInOrigin && parsed.pathname === Routes.SignInContinue;
    } catch {
      return false;
    }
  }), { timeout: pageCompileTimeoutMilliseconds }).toBeTruthy().then(() => navigation.navigatedUrls.find((url) => {
    try {
      const parsed = new URL(url);
      return parsed.origin === signInOrigin && parsed.pathname === Routes.SignInContinue;
    } catch {
      return false;
    }
  })!);
  const handoffUrl = await completeLandingSignIn(page, navigation.navigatedUrls);
  await page.waitForURL((url) => url.origin === landingOrigin && url.pathname !== Routes.Handoff && !url.pathname.startsWith(Routes.Login.Index), {
    waitUntil: "domcontentloaded", timeout: pageCompileTimeoutMilliseconds,
  });
  await expect.poll(async () => (await callApi(page, "GET", sessionPath)).status, { timeout: pageCompileTimeoutMilliseconds }).toBe(200);
  return { continueUrl, handoffUrl, issuingCookieHeader: navigation.issuingCookieHeader };
}

async function signInSeededClinicOwner(page: Page): Promise<SignInResult> {
  expect(ownerUsername, `${twoHostE2eOwnerUsernameEnvironmentKey} must be supplied by the two-host runner`).toBeTruthy();
  expect(password, `${twoHostE2ePasswordEnvironmentKey} must be supplied by the two-host runner`).toBeTruthy();
  const navigation = await observeSignInNavigation(page);
  await page.goto(`${signInOrigin}${Routes.Login.Password}`, { waitUntil: "domcontentloaded", timeout: pageCompileTimeoutMilliseconds });
  await expect(page.locator("form")).toHaveAttribute("method", "post", { timeout: pageCompileTimeoutMilliseconds });
  await page.locator('input[name="username"]').fill(ownerUsername!);
  await page.locator('input[name="password"]').fill(password!);
  const [loginResponse] = await Promise.all([
    page.waitForResponse((response) => response.url().endsWith(passwordLoginPath) && response.request().method() === "POST"),
    page.locator('button[type="submit"]').click(),
  ]);
  expect(loginResponse.status()).toBe(200);
  const continueUrl = await expect.poll(() => navigation.navigatedUrls.find((url) => {
    try {
      const parsed = new URL(url);
      return parsed.origin === signInOrigin && parsed.pathname === Routes.SignInContinue;
    } catch {
      return false;
    }
  }), { timeout: pageCompileTimeoutMilliseconds }).toBeTruthy().then(() => navigation.navigatedUrls.find((url) => {
    try {
      const parsed = new URL(url);
      return parsed.origin === signInOrigin && parsed.pathname === Routes.SignInContinue;
    } catch {
      return false;
    }
  })!);
  const handoffUrl = await completeLandingSignIn(page, navigation.navigatedUrls);
  await page.waitForURL((url) => url.origin === landingOrigin && url.pathname !== Routes.Handoff && !url.pathname.startsWith(Routes.Login.Index), {
    waitUntil: "domcontentloaded", timeout: pageCompileTimeoutMilliseconds,
  });
  await expect.poll(async () => (await callApi(page, "GET", sessionPath)).status, { timeout: pageCompileTimeoutMilliseconds }).toBe(200);
  return { continueUrl, handoffUrl, issuingCookieHeader: navigation.issuingCookieHeader };
}

async function expectSourceSessionActive(context: BrowserContext, issuingCookieHeader: string, clientIp: string): Promise<void> {
  const apiOrigin = process.env[twoHostE2eApiOriginEnvironmentKey];
  expect(apiOrigin, `${twoHostE2eApiOriginEnvironmentKey} must be supplied by the two-host runner`).toBeTruthy();
  expect(issuingCookieHeader, "the sign-in host session cookie is captured before handoff").toBeTruthy();
  const response = await context.request.get(new URL(sessionPath, apiOrigin!).href, {
    headers: {
      Cookie: issuingCookieHeader,
      [TENANT_HOST_HEADER]: new URL(signInOrigin).host,
      [forwardedForHeaderName]: clientIp,
    },
    ignoreHTTPSErrors: true,
  });
  expect(response.status()).toBe(200);
}

async function captureProofScreenshot(page: Page): Promise<void> {
  const configuredPath = process.env[twoHostE2eProofScreenshotEnvironmentKey] ?? "artifacts/two-host-e2e/platform-home.png";
  const screenshotPath = existsSync(configuredPath) ? configuredPath.replace(/\.png$/i, "-platform-home.png") : configuredPath;
  mkdirSync(dirname(screenshotPath), { recursive: true });
  expect(existsSync(screenshotPath), `refusing to overwrite retained evidence at ${screenshotPath}`).toBe(false);
  await page.mouse.move(0, 0);
  await page.screenshot({ path: screenshotPath, fullPage: true, animations: "disabled" });
}

test("an anonymous visitor to the sign-in host root lands on login", async ({ browser }) => {
  test.setTimeout(180_000);
  const context = await browser.newContext({ extraHTTPHeaders: { [forwardedForHeaderName]: anonymousClientIp } });
  try {
    await configureContext(context);
    const page = await context.newPage();
    await page.goto(`${signInOrigin}${Routes.Root}`, { waitUntil: "domcontentloaded", timeout: pageCompileTimeoutMilliseconds });
    await expect(page).toHaveURL(`${signInOrigin}${Routes.Login.Index}`, { timeout: pageCompileTimeoutMilliseconds });
  } finally {
    await context.close();
  }
});

test("an authenticated sign-in host visitor continues with the safe return intent", async ({ browser }) => {
  test.setTimeout(180_000);
  expect(ownerUsername).toBeTruthy();
  expect(password).toBeTruthy();
  const context = await browser.newContext({ extraHTTPHeaders: { [forwardedForHeaderName]: authenticatedLoginClientIp } });
  const page = await context.newPage();
  let signedIn = false;
  try {
    await configureContext(context);
    await page.goto(`${signInOrigin}${Routes.Login.Index}`, { waitUntil: "domcontentloaded", timeout: pageCompileTimeoutMilliseconds });
    const login = await callApi(page, "POST", passwordLoginPath, {
      userNameOrEmail: ownerUsername,
      password,
      returnUrl: null,
    });
    expect(login.status).toBe(200);
    signedIn = true;
    await page.route((url) => url.pathname === signInHomeHandoffPath, (route) => route.abort());
    const loginUrl = new URL(Routes.Login.Index, signInOrigin);
    loginUrl.searchParams.set(signInReturnUrlQueryKey, returnIntent);
    await page.goto(loginUrl.toString(), { waitUntil: "domcontentloaded", timeout: pageCompileTimeoutMilliseconds });
    await expect(page).toHaveURL((url) => url.origin === signInOrigin
      && url.pathname === Routes.SignInContinue
      && url.searchParams.get(signInReturnUrlQueryKey) === returnIntent, { timeout: pageCompileTimeoutMilliseconds });
  } finally {
    if (signedIn) await callApi(page, "POST", generatedPath(authenticationLogoutPost), { logoutId: null, returnUrl: null });
    await context.close();
  }
});

test("a fresh phone sign-in with no workspaces reaches platform home and can explicitly request onboarding", async ({ browser }) => {
  test.setTimeout(600_000);
  expect(smsCapturePath, `${twoHostE2eSmsCapturePathEnvironmentKey} must be supplied by the two-host runner`).toBeTruthy();
  const context = await browser.newContext({ extraHTTPHeaders: { [forwardedForHeaderName]: emptyHomeClientIp } });
  try {
    await configureContext(context);
    const page = await context.newPage();
    const signedIn = await signInWithPhone(page, `912${randomDigits(7)}`);
    expect(new URL(signedIn.handoffUrl).host).toBe(landingHost);
    expect(new URL(signedIn.handoffUrl).searchParams.has(signInReturnUrlQueryKey)).toBe(false);
    const session = await callApi(page, "GET", sessionPath);
    expect(session).toMatchObject({ status: 200, body: { isAuthenticated: true } });

    const directory = await callApi(page, "GET", generatedPath(landingListWorkspaces));
    expect(directory.status).toBe(200);
    const emptyDirectory = directory.body as { workspaces: Array<unknown>; apps: Array<{ appId: string; availability: string; canCreate: boolean }> };
    expect(emptyDirectory.workspaces).toEqual([]);
    const clinicApp = emptyDirectory.apps.find((app) => app.appId === expectedClinicAppId);
    expect(clinicApp).toMatchObject({ availability: LandingAppAvailability.Live, canCreate: true });
    const appsAvailableForCreation = emptyDirectory.apps.filter((app) => app.canCreate);
    expect(appsAvailableForCreation).toHaveLength(1);
    expect(appsAvailableForCreation[0].appId).toBe(expectedClinicAppId);

    const onboardingPath = generatedPath(landingIssueOnboardingHandoff);
    const unknownAppHandoff = await callApi(page, "POST", onboardingPath, {
      category: null,
      answers: null,
      appId: crypto.randomUUID(),
    });
    expect(unknownAppHandoff.status).toBe(400);
    const onboardingHandoff = await callApi(page, "POST", onboardingPath, {
      category: null,
      answers: null,
      appId: clinicApp!.appId,
    });
    expect(onboardingHandoff.status).toBe(200);
    expect((onboardingHandoff.body as { hostname: string }).hostname).toBe(onboardingHostname);
    await expectSourceSessionActive(context, signedIn.issuingCookieHeader, emptyHomeClientIp);
  } finally {
    await context.close();
  }
});

test("a person with one clinic reaches platform home before choosing and opening that clinic", async ({ browser }) => {
  test.setTimeout(600_000);
  const context = await browser.newContext({ extraHTTPHeaders: { [forwardedForHeaderName]: clinicHomeClientIp } });
  try {
    await configureContext(context, [defaultClinicOrigin]);
    const homePage = await context.newPage();
    const signedIn = await signInSeededClinicOwner(homePage);
    expect(new URL(signedIn.handoffUrl).host).toBe(landingHost);
    const directoryResult = await callApi(homePage, "GET", generatedPath(landingListWorkspaces));
    expect(directoryResult.status).toBe(200);
    const directory = directoryResult.body as {
      workspaces: Array<{ tenantId: string; appId: string; hostname: string }>;
      apps: Array<{ appId: string; availability: string; canCreate: boolean }>;
    };
    expect(directory.workspaces).toHaveLength(1);
    const [clinic] = directory.workspaces;
    expect(clinic.appId).toBe(expectedClinicAppId);
    expect(clinic.hostname).toBe(new URL(defaultClinicOrigin).hostname);
    expect(directory.apps.find((app) => app.appId === expectedClinicAppId)).toMatchObject({
      availability: LandingAppAvailability.Live,
      canCreate: true,
    });

    const workspaceHandoffPath = generatedPath(landingIssueWorkspaceHandoff).replace(/\$\{[^}]+\}/, clinic.tenantId);
    const workspaceHandoff = await callApi(homePage, "POST", workspaceHandoffPath);
    expect(workspaceHandoff.status).toBe(200);
    const handoff = workspaceHandoff.body as { hostname: string; token: string; handoffUrl: string };
    expect(handoff.hostname).toBe(clinic.hostname);
    const unknownTenantPath = generatedPath(landingIssueWorkspaceHandoff).replace(/\$\{[^}]+\}/, crypto.randomUUID());
    expect((await callApi(homePage, "POST", unknownTenantPath)).status).toBe(404);

    const clinicOrigin = `http://${handoff.hostname}:3005`;
    await context.addCookies([{ name: localeCookieName, value: locale, url: clinicOrigin }]);
    const clinicPage = await context.newPage();
    expect(handoff.token).toBe("");
    expect(new URL(handoff.handoffUrl).origin).toBe(clinicOrigin);
    await clinicPage.goto(handoff.handoffUrl, { waitUntil: "domcontentloaded", timeout: pageCompileTimeoutMilliseconds });
    await clinicPage.waitForURL((url) => url.origin === clinicOrigin && url.pathname !== Routes.Handoff && !url.pathname.startsWith(Routes.Login.Index), {
      waitUntil: "domcontentloaded", timeout: pageCompileTimeoutMilliseconds,
    });
    const clinicSession = await callApi(clinicPage, "GET", sessionPath);
    expect(clinicSession).toMatchObject({ status: 200, body: { isAuthenticated: true } });
    // Home and workspace belong to the same central browser family.
    expect(await callApi(homePage, "GET", sessionPath)).toMatchObject({ status: 200 });
    await clinicPage.waitForLoadState("networkidle");
    await expect(clinicPage.getByText(messages.home.dashboard.loadingSurgeries)).toBeHidden();
    await expect(clinicPage.getByText(clinicMessages.appointments.visits.loading)).toBeHidden();
    await captureProofScreenshot(clinicPage);
    await expectSourceSessionActive(context, signedIn.issuingCookieHeader, clinicHomeClientIp);
  } finally {
    await context.close();
  }
});

test("a safe relative return intent survives phone sign-in from the login index", async ({ browser }) => {
  test.setTimeout(600_000);
  expect(smsCapturePath, `${twoHostE2eSmsCapturePathEnvironmentKey} must be supplied by the two-host runner`).toBeTruthy();
  const context = await browser.newContext({ extraHTTPHeaders: { [forwardedForHeaderName]: safeReturnClientIp } });
  try {
    await configureContext(context);
    const page = await context.newPage();
    const signedIn = await signInWithPhone(page, `912${randomDigits(7)}`, returnIntent, true);
    expect(new URL(signedIn.continueUrl).searchParams.get(signInReturnUrlQueryKey)).toBe(returnIntent);
    expect(new URL(page.url()).pathname).toBe(returnIntent);
  } finally {
    await context.close();
  }
});

test("a protocol-relative return intent is dropped before the landing handoff", async ({ browser }) => {
  test.setTimeout(600_000);
  expect(smsCapturePath, `${twoHostE2eSmsCapturePathEnvironmentKey} must be supplied by the two-host runner`).toBeTruthy();
  const context = await browser.newContext({ extraHTTPHeaders: { [forwardedForHeaderName]: unsafeReturnClientIp } });
  try {
    await configureContext(context);
    const page = await context.newPage();
    const signedIn = await signInWithPhone(page, `912${randomDigits(7)}`, unsafeReturnIntent, true);
    expect(new URL(signedIn.handoffUrl).searchParams.has(signInReturnUrlQueryKey)).toBe(false);
  } finally {
    await context.close();
  }
});

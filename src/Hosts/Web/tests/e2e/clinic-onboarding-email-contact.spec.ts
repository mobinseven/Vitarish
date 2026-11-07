import { expect, test, type Page } from "@playwright/test";
import crypto from "node:crypto";
import { existsSync, mkdirSync, readFileSync, statSync } from "node:fs";
import { dirname } from "node:path";
import { Routes } from "../../apps/private-practice-owner/src/app/routes";
import { LandingRoutes } from "../../apps/vitarish-landing/src/app/routes";
import { enMessages } from "../../apps/private-practice-owner/src/localization/messages/en";
import { faMessages } from "../../apps/private-practice-owner/src/localization/messages/fa";
import {
  twoHostE2eLocaleEnvironmentKey,
  twoHostE2eLandingOrigin,
  twoHostE2eProofScreenshotEnvironmentKey,
  twoHostE2eSignInOrigin,
} from "../../scripts/two-host-e2e-artifacts.mjs";
import { REQUEST_ID_HEADER, TENANT_HOST_HEADER } from "../../shared/api-client/src/apiFetch";
import { authenticationAntiforgeryGet } from "../../shared/auth/src/api/generated/authentication/authentication";
import { clinicMessageCatalogs } from "../../shared/clinic/src/localization/catalogs";
import { onboardingHandoffUrl, tenantHandoffPath } from "../../shared/journey/src/onboardingHost";
import { localeCookieName } from "../../shared/localization/src/localeCookie";
import type { SupportedLocale } from "../../shared/localization/src/locales";
import { landingIssueOnboardingHandoff } from "../../shared/platform/src/api/generated/landing/landing";
import type { LandingOnboardingHandoffRequest, LandingOnboardingHandoffResponse } from "../../shared/platform/src/api/generated/landing/model";
import { forwardedForHeaderName } from "./signInHost";
import { generatedOperationPath } from "./generatedOperationPath";

const locale: SupportedLocale = process.env[twoHostE2eLocaleEnvironmentKey] === "fa" ? "fa" : "en";
const ownerMessages = { en: enMessages, fa: faMessages }[locale];
const authMessages = ownerMessages.auth;
const setupMessages = ownerMessages.journey.setup;
const clinicMessages = clinicMessageCatalogs[locale].clinic;

const smtpCapturePath = process.env.VITARISH_E2E_SMTP_CAPTURE_PATH;
const landingOrigin = twoHostE2eLandingOrigin;
const onboardingOrigin = "http://start.localhost:3005";
// E2eFixtureSeeder.EmailOnlyClinicCreatorEmail: confirmed email, no phone, no role.
const creatorEmail = "e2e-email-only-clinic-creator@example.test";
const creatorClientIp = "198.51.100.91";
const pageCompileTimeoutMilliseconds = 120_000;
const onboardingHandoffPath = generatedOperationPath(landingIssueOnboardingHandoff);
const antiforgeryPath = generatedOperationPath(authenticationAntiforgeryGet);
const onboardingHandoffRequest = { category: null, answers: [] } satisfies LandingOnboardingHandoffRequest;

test.use({ actionTimeout: 60_000 });

function escapeRegExp(text: string) {
  return text.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

/** Matches a field's accessible name, which carries a trailing "*" when the field is required. */
function fieldName(label: string) {
  return new RegExp(`^${escapeRegExp(label)}\\*?$`);
}

async function readDeliveredEmailCode(previousModifiedAt: number): Promise<string> {
  await expect.poll(() => Boolean(smtpCapturePath && existsSync(smtpCapturePath) && statSync(smtpCapturePath).mtimeMs > previousModifiedAt)).toBe(true);
  const message = readFileSync(smtpCapturePath!, "utf8");
  const code = /Your verification code is:\s*(?:<strong>)?([^<\s]+)(?:<\/strong>)?/i.exec(message)?.[1];
  expect(code).toEqual(expect.any(String));
  return code!;
}

async function requestOnboardingHandoff(page: Page): Promise<LandingOnboardingHandoffResponse> {
  const result = await page.evaluate(async ({ tenantHostHeader, requestIdHeader, tokenPath, handoffPath, requestId, requestBody }) => {
    const headers = { [tenantHostHeader]: window.location.host, [requestIdHeader]: requestId };
    const antiforgeryResponse = await fetch(tokenPath, { credentials: "include", headers });
    if (!antiforgeryResponse.ok) return { status: antiforgeryResponse.status, body: null };

    const antiforgery = (await antiforgeryResponse.json()) as { headerName: string; requestToken: string };
    const response = await fetch(handoffPath, {
      method: "POST",
      credentials: "include",
      headers: { ...headers, "Content-Type": "application/json", [antiforgery.headerName]: antiforgery.requestToken },
      body: JSON.stringify(requestBody),
    });
    const text = await response.text();
    return { status: response.status, body: text ? JSON.parse(text) : null };
  }, {
    tenantHostHeader: TENANT_HOST_HEADER,
    requestIdHeader: REQUEST_ID_HEADER,
    tokenPath: antiforgeryPath,
    handoffPath: onboardingHandoffPath,
    requestId: `e2e-email-contact-handoff-${crypto.randomUUID()}`,
    requestBody: onboardingHandoffRequest,
  });
  expect(result.status, "the signed-in landing host must issue the onboarding handoff").toBe(200);
  return result.body as LandingOnboardingHandoffResponse;
}

test("an account with only a confirmed email signs in by email code and creates a clinic", async ({ context, page }) => {
  test.setTimeout(600_000);
  expect(smtpCapturePath, "VITARISH_E2E_SMTP_CAPTURE_PATH must be supplied by the two-host runner").toBeTruthy();

  const suffix = crypto.randomBytes(3).toString("hex");
  const subdomain = `mail-${suffix}`;
  const clinicHostname = `${subdomain}.localhost`;
  const clinicName = `Email Contact Clinic ${suffix}`;

  await page.route(/^http:\/\/[^/]*localhost:3005\//, (route) =>
    route.continue({ headers: { ...route.request().headers(), [forwardedForHeaderName]: creatorClientIp } }),
  );
  await context.addCookies([
    { name: localeCookieName, value: locale, url: landingOrigin },
    { name: localeCookieName, value: locale, url: onboardingOrigin },
  ]);

  // 1. Sign in on the shared sign-in host with the emailed code; the account has no phone at all.
  await page.goto(`${twoHostE2eSignInOrigin}${Routes.Login.Email}`, { waitUntil: "domcontentloaded" });
  await page.getByRole("textbox", { name: fieldName(authMessages.otp.emailLabel) }).fill(creatorEmail, {
    timeout: pageCompileTimeoutMilliseconds,
  });
  const previousModifiedAt = existsSync(smtpCapturePath!) ? statSync(smtpCapturePath!).mtimeMs : 0;
  const [beginResponse] = await Promise.all([
    page.waitForResponse((response) => response.url().endsWith("/api/modules/authentication/login/email/begin")),
    page.getByRole("button", { name: authMessages.otp.sendAction, exact: true }).click(),
  ]);
  expect(beginResponse.status()).toBe(200);
  await page.getByLabel(fieldName(authMessages.otp.codeLabel)).fill(await readDeliveredEmailCode(previousModifiedAt));
  const [completeResponse] = await Promise.all([
    page.waitForResponse((response) => response.url().endsWith("/api/modules/authentication/login/email/complete")),
    page.getByRole("button", { name: authMessages.otp.verifyAction, exact: true }).click(),
  ]);
  expect(completeResponse.status()).toBe(200);

  await page.waitForURL((url) => url.origin === landingOrigin && url.pathname === LandingRoutes.PlatformHome, {
    waitUntil: "domcontentloaded",
    timeout: pageCompileTimeoutMilliseconds,
  });

  // 2. Continue to the onboarding host and create the clinic.
  const handoff = await requestOnboardingHandoff(page);
  await page.goto(onboardingHandoffUrl(onboardingOrigin, handoff.token), {
    waitUntil: "domcontentloaded",
    timeout: pageCompileTimeoutMilliseconds,
  });
  await page.waitForURL((url) => url.host === new URL(onboardingOrigin).host && !url.pathname.startsWith(tenantHandoffPath), {
    timeout: pageCompileTimeoutMilliseconds,
  });
  await expect(page.getByRole("heading", { name: setupMessages.title })).toBeVisible({ timeout: pageCompileTimeoutMilliseconds });

  await page.getByRole("textbox", { name: fieldName(setupMessages.name) }).fill(clinicName);
  await page.getByRole("textbox", { name: fieldName(setupMessages.address) }).fill(subdomain);
  await expect(page.getByText(setupMessages.availability.Available)).toBeVisible({ timeout: pageCompileTimeoutMilliseconds });
  await page.getByRole("button", { name: setupMessages.next, exact: true }).click();
  await page.getByRole("button", { name: setupMessages.specialty }).click();
  await page.getByRole("option", { name: clinicMessages.specialties.dermatology }).click();
  const [createResponse] = await Promise.all([
    page.waitForResponse((response) => response.url().endsWith("/api/modules/parties/onboarding/tenants") && response.request().method() === "POST"),
    page.getByRole("button", { name: setupMessages.create, exact: true }).click(),
  ]);

  // An email-only account is allowed: it is created, not refused with 403 for a missing phone.
  expect(createResponse.status()).toBe(201);
  await expect(page.getByRole("heading", { name: setupMessages.ready.title })).toBeVisible({ timeout: pageCompileTimeoutMilliseconds });
  await expect(page.getByText(clinicHostname, { exact: true })).toBeVisible();

  const proof = process.env[twoHostE2eProofScreenshotEnvironmentKey]?.replace(/\.png$/i, "-email-contact-clinic-ready.png");
  if (proof) {
    mkdirSync(dirname(proof), { recursive: true });
    expect(existsSync(proof), `refusing to overwrite retained evidence at ${proof}`).toBe(false);
    await page.mouse.move(0, 0);
    await page.screenshot({ path: proof, fullPage: true, animations: "disabled" });
  }
});

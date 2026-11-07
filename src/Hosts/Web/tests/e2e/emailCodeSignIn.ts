import { expect, type Page } from "@playwright/test";
import { existsSync, readFileSync, statSync } from "node:fs";
import { Routes } from "../../apps/private-practice-owner/src/app/routes";
import { LandingRoutes } from "../../apps/vitarish-landing/src/app/routes";
import { enMessages } from "../../apps/private-practice-owner/src/localization/messages/en";
import { faMessages } from "../../apps/private-practice-owner/src/localization/messages/fa";
import {
  twoHostE2eLandingOrigin,
  twoHostE2eLocaleEnvironmentKey,
  twoHostE2eSignInOrigin,
} from "../../scripts/two-host-e2e-artifacts.mjs";

const authMessages = ({ en: enMessages, fa: faMessages }[process.env[twoHostE2eLocaleEnvironmentKey] === "fa" ? "fa" : "en"]).auth;
const smtpCapturePath = process.env.VITARISH_E2E_SMTP_CAPTURE_PATH;
const pageCompileTimeoutMilliseconds = 120_000;

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

/**
 * Signs an account in on the shared sign-in host with the emailed one-time code, then waits for the
 * platform home on the landing host. The two-host runner captures the email, so no mailbox is involved.
 */
export async function signInWithEmailCodeOnSharedHost(page: Page, email: string): Promise<void> {
  expect(smtpCapturePath, "VITARISH_E2E_SMTP_CAPTURE_PATH must be supplied by the two-host runner").toBeTruthy();
  await page.goto(`${twoHostE2eSignInOrigin}${Routes.Login.Email}`, { waitUntil: "domcontentloaded" });
  await page.getByRole("textbox", { name: fieldName(authMessages.otp.emailLabel) }).fill(email, {
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

  await page.waitForURL((url) => url.origin === twoHostE2eLandingOrigin && url.pathname === LandingRoutes.PlatformHome, {
    waitUntil: "domcontentloaded",
    timeout: pageCompileTimeoutMilliseconds,
  });
}

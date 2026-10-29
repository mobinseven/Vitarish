import { existsSync, readFileSync, statSync } from "node:fs";
import crypto from "node:crypto";
import { expect, test, type Page } from "@playwright/test";
import { Routes as ClinicRoutes } from "../../apps/private-practice-owner/src/app/routes";
import { enMessages } from "../../apps/private-practice-owner/src/localization/messages/en";
import { twoHostE2eSmsCapturePathEnvironmentKey } from "../../scripts/two-host-e2e-artifacts.mjs";
import { forwardedForHeaderName, signInToClinic } from "./signInHost";

const privatePracticeOrigin = process.env.VITARISH_E2E_WEB_ORIGIN ?? "http://localhost:3005";
const smsCapturePath = process.env[twoHostE2eSmsCapturePathEnvironmentKey];
const password = process.env.VITARISH_E2E_PASSWORD;
// E2eFixtureSeeder.PhoneChangeUserName: a tenant user no other spec signs in.
const subjectUsername = "e2e-phone-change-user";
const messages = enMessages;
const persianDigits = "۰۱۲۳۴۵۶۷۸۹";

type CapturedSms = {
  Recipient: string;
  Text: string;
};

function toPersianDigits(latin: string): string {
  return latin.replace(/\d/g, (digit) => persianDigits[Number(digit)]);
}

async function readDeliveredSmsCode(previousModifiedAt: number, nationalDigits: string): Promise<string> {
  await expect.poll(() => {
    return Boolean(smsCapturePath && existsSync(smsCapturePath) && statSync(smsCapturePath).mtimeMs > previousModifiedAt);
  }).toBe(true);

  const lines = readFileSync(smsCapturePath!, "utf8")
    .trim()
    .split(/\r?\n/)
    .filter(Boolean)
    .map((line) => JSON.parse(line) as CapturedSms);
  const code = [...lines].reverse().find((entry) => entry.Recipient.endsWith(nationalDigits))?.Text;
  expect(code).toEqual(expect.stringMatching(/^\d+$/));
  return code!;
}

async function signIn(page: Page) {
  await signInToClinic(page, subjectUsername, password!, privatePracticeOrigin);
}

// A number typed with Persian digits in local format is converted by the server
// before the challenge: the code goes to the international number, and verifying
// it stores that number on the account.
test("changing the phone number accepts a local number typed with Persian digits", async ({ page }) => {
  expect(smsCapturePath, `${twoHostE2eSmsCapturePathEnvironmentKey} must be supplied by the two-host runner`).toBeTruthy();
  expect(password, "VITARISH_E2E_PASSWORD must be supplied by the disposable E2E environment").toBeTruthy();
  // Keep this scenario on its own OtpBegin rate-limit partition.
  await page.setExtraHTTPHeaders({ [forwardedForHeaderName]: "198.51.100.120" });
  const nationalDigits = `912${Array.from({ length: 7 }, () => crypto.randomInt(0, 10)).join("")}`;

  await signIn(page);
  await page.goto(`${privatePracticeOrigin}${ClinicRoutes.Account.Phone}`, { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { name: messages.account.phone.title, exact: true })).toBeVisible({ timeout: 30_000 });

  const previousModifiedAt = existsSync(smsCapturePath!) ? statSync(smsCapturePath!).mtimeMs : 0;
  await page.getByRole("textbox", { name: messages.account.phone.newPhone }).fill(toPersianDigits(`0${nationalDigits}`));
  await page.getByRole("button", { name: messages.auth.otp.sendAction, exact: true }).click();
  const code = await readDeliveredSmsCode(previousModifiedAt, nationalDigits);

  await expect(page.getByLabel(messages.auth.otp.codeLabel)).toBeVisible({ timeout: 30_000 });
  await page.getByLabel(messages.auth.otp.codeLabel).fill(code);
  await page.getByRole("button", { name: messages.auth.otp.verifyAction, exact: true }).click();
  await expect(page.getByText(messages.account.phone.updateSucceeded, { exact: true })).toBeVisible({ timeout: 30_000 });

  await page.reload({ waitUntil: "domcontentloaded" });
  await expect(page.getByText(`+98${nationalDigits}`, { exact: true })).toBeVisible({ timeout: 30_000 });
});

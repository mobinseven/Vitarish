import { expect, test, type Page } from "@playwright/test";
import { Routes } from "../../apps/private-practice-owner/src/app/routes";
import { enMessages } from "../../apps/private-practice-owner/src/localization/messages/en";
import {
  twoHostE2eClinicOrigin,
  twoHostE2eOwnerUsernameEnvironmentKey,
  twoHostE2ePasswordEnvironmentKey,
  twoHostE2eSignInOrigin,
} from "../../scripts/two-host-e2e-artifacts.mjs";
import { authenticationLoginPassword } from "../../shared/auth/src/api/generated/authentication/authentication";
import { generatedOperationPath } from "./generatedOperationPath";

// Signing in from any clinic page brings the person back to that exact page.
const ownerUsername = process.env[twoHostE2eOwnerUsernameEnvironmentKey];
const ownerPassword = process.env[twoHostE2ePasswordEnvironmentKey];
const passwordLoginPath = generatedOperationPath(authenticationLoginPassword);
const signInTimeoutMilliseconds = 120_000;

/** Signs in with a password on the central sign-in host, keeping the return target the redirect carried. */
async function signInWhereSent(page: Page) {
  await page.waitForURL((url) => url.origin === twoHostE2eSignInOrigin && url.pathname.startsWith(Routes.Login.Index), {
    timeout: signInTimeoutMilliseconds,
  });
  const sentTo = new URL(page.url());
  expect(sentTo.search, "the sign-in host carries where to continue").not.toBe("");
  await page.goto(`${twoHostE2eSignInOrigin}${Routes.Login.Password}${sentTo.search}`, { waitUntil: "domcontentloaded" });
  await expect(page.locator('button[type="submit"]')).toBeEnabled({ timeout: 60_000 });
  await page.locator('input[name="username"]').fill(ownerUsername!);
  await page.locator('input[name="password"]').fill(ownerPassword!);
  const [response] = await Promise.all([
    page.waitForResponse((candidate) => candidate.url().endsWith(passwordLoginPath) && candidate.request().method() === "POST"),
    page.locator('button[type="submit"]').click(),
  ]);
  expect(response.status()).toBe(200);
}

for (const target of [
  { name: "the clinic's front page", path: Routes.Home },
  { name: "another clinic page", path: Routes.Clinic.Patients.Index },
]) {
  test(`a signed-out visitor to ${target.name} signs in and returns to it`, async ({ browser }) => {
    expect(ownerUsername).toBeTruthy();
    expect(ownerPassword).toBeTruthy();
    const context = await browser.newContext({ ignoreHTTPSErrors: true });
    const page = await context.newPage();
    try {
      await page.goto(`${twoHostE2eClinicOrigin}${target.path}`, { waitUntil: "domcontentloaded" });
      // No failed-check card for a missing session: the visitor goes straight to sign-in.
      await signInWhereSent(page);
      await expect(page.getByText(enMessages.journey.error.title)).toHaveCount(0);

      const expected = new URL(target.path, twoHostE2eClinicOrigin);
      await page.waitForURL(
        (url) => url.origin === expected.origin && url.pathname === expected.pathname && url.search === expected.search,
        { timeout: signInTimeoutMilliseconds },
      );
    } finally {
      await context.close();
    }
  });
}

import { expect, type Page, type Response } from "@playwright/test";
import { Routes } from "../../apps/private-practice-owner/src/app/routes";
import { twoHostE2eClinicOrigin, twoHostE2eSignInOrigin as signInOrigin } from "../../scripts/two-host-e2e-artifacts.mjs";
import { authenticationLoginPassword } from "../../shared/auth/src/api/generated/authentication/authentication";
import { generatedOperationPath } from "./generatedOperationPath";

/** The E2E clinic's own host. */
export const defaultClinicOrigin = twoHostE2eClinicOrigin;
export const forwardedForHeaderName = "X-Forwarded-For";
const handoffTimeoutMilliseconds = 120_000;
const passwordLoginPath = generatedOperationPath(authenticationLoginPassword);

/** Submits the password form on the sign-in host and returns the sign-in response. */
export async function submitPasswordOnSignInHost(page: Page, username: string, password: string): Promise<Response> {
  await page.goto(`${signInOrigin}${Routes.Login.Password}`, { waitUntil: "domcontentloaded" });
  await expect(page.locator("form")).toHaveAttribute("method", "post", { timeout: 60_000 });
  await expect(page.locator('button[type="submit"]')).toBeEnabled({ timeout: 60_000 });
  await page.locator('input[name="username"]').fill(username);
  await page.locator('input[name="password"]').fill(password);
  const [response] = await Promise.all([
    page.waitForResponse((candidate) => candidate.url().endsWith(passwordLoginPath) && candidate.request().method() === "POST"),
    page.locator('button[type="submit"]').click(),
  ]);
  return response;
}

/**
 * Follows a signed-in sign-in host to the clinic at `clinicOrigin`: one clinic opens by itself,
 * several are listed and the one at that host is opened. Resolves once its handoff is redeemed.
 */
export async function continueToClinic(page: Page, clinicOrigin = defaultClinicOrigin): Promise<void> {
  const onClinic = (url: URL) => url.origin === clinicOrigin;
  const clinicRow = page.getByRole("listitem").filter({ has: page.getByText(new URL(clinicOrigin).hostname, { exact: true }) });
  const needsChoice = await Promise.race([
    page.waitForURL(onClinic, { waitUntil: "commit", timeout: handoffTimeoutMilliseconds }).then(() => false),
    // One clinic can render briefly while it opens automatically; only a second row needs a choice.
    page.getByRole("listitem").nth(1).waitFor({ state: "visible", timeout: handoffTimeoutMilliseconds }).then(() => true),
  ]);
  if (needsChoice) await clinicRow.getByRole("button").click();
  await page.waitForURL((url) => onClinic(url) && url.pathname !== Routes.Handoff, {
    waitUntil: "domcontentloaded",
    timeout: handoffTimeoutMilliseconds,
  });
  // The URL changes before the destination journey/session gate finishes. Its dashboard heading
  // confirms that initialization is complete before a caller starts another navigation.
  await expect(page.getByRole("main").getByRole("heading", { level: 1 })).toBeVisible({
    timeout: handoffTimeoutMilliseconds,
  });
}

/** Signs in with a password on the sign-in host and lands signed in on the clinic at `clinicOrigin`. */
export async function signInToClinic(page: Page, username: string, password: string, clinicOrigin = defaultClinicOrigin): Promise<void> {
  const response = await submitPasswordOnSignInHost(page, username, password);
  expect(response.status()).toBe(200);
  await continueToClinic(page, clinicOrigin);
}

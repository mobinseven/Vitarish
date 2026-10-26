import { expect, test } from "@playwright/test";
import { Routes as ClinicRoutes } from "../../apps/private-practice-owner/src/app/routes";
import { twoHostE2eSignInOrigin } from "../../scripts/two-host-e2e-artifacts.mjs";


test("malformed phone login is rejected before a verification challenge is issued", async ({ page }) => {
  // Keep this request on its own OtpBegin rate-limit partition; the shared
  // client IP is exhausted by the other recovery/login scenarios.
  await page.setExtraHTTPHeaders({ "X-Forwarded-For": "198.51.100.201" });
  // Phone sign-in lives on the shared sign-in host.
  await page.goto(`${twoHostE2eSignInOrigin}${ClinicRoutes.Login.Phone}`, { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { name: "Phone Sign In", exact: true })).toBeVisible({ timeout: 30_000 });

  const phone = page.getByLabel("Phone Number");
  await phone.fill("not-a-number");
  const [beginResponse] = await Promise.all([
    page.waitForResponse((response) => response.url().endsWith("/api/modules/authentication/login/phone/begin")),
    page.getByRole("button", { name: "Send Code", exact: true }).click(),
  ]);

  expect(beginResponse.status()).toBe(400);
  await expect(page.getByText("Enter a valid mobile number.", { exact: true })).toBeVisible();
  await expect(phone).toHaveValue("not-a-number");
  await expect(page.getByLabel("Verification Code")).toHaveCount(0);
});

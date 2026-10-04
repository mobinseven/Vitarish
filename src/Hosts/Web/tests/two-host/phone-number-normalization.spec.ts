import { expect, test } from "@playwright/test";
import { Routes as ClinicRoutes } from "../../apps/private-practice-owner/src/app/routes";

const privatePracticeOrigin = process.env.VITARISH_E2E_WEB_ORIGIN ?? "http://localhost:3005";

test("malformed phone login is rejected before a verification challenge is issued", async ({ page }) => {
  // Keep this request on its own OtpBegin rate-limit partition; the shared
  // client IP is exhausted by the other recovery/login scenarios.
  await page.setExtraHTTPHeaders({ "X-Forwarded-For": "198.51.100.201" });
  await page.goto(`${privatePracticeOrigin}${ClinicRoutes.Login.Phone}`, { waitUntil: "domcontentloaded" });
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

import { existsSync, readFileSync, statSync } from "node:fs";
import { expect, test } from "@playwright/test";
import { Routes as ClinicRoutes } from "../../apps/private-practice-owner/src/app/routes";

const privatePracticeOrigin = process.env.VITARISH_E2E_WEB_ORIGIN ?? "http://localhost:3005";
const smtpCapturePath = process.env.VITARISH_E2E_SMTP_CAPTURE_PATH;

async function readDeliveredCode(previousModifiedAt: number): Promise<string> {
  await expect.poll(() => {
    return Boolean(smtpCapturePath && existsSync(smtpCapturePath) && statSync(smtpCapturePath).mtimeMs > previousModifiedAt);
  }).toBe(true);

  const message = readFileSync(smtpCapturePath!, "utf8");
  const code = /Your verification code is:\s*(?:<strong>)?([^<\s]+)(?:<\/strong>)?/i.exec(message)?.[1];
  expect(code).toEqual(expect.any(String));
  return code!;
}

// Email-code sign-in auto-creates the account, so this scenario owns a brand
// new user with no global role and no tenant membership and needs no other
// spec's state. Its own client IP keeps the begin rate-limit partition shared
// with no earlier scenario.
test("an account with no role or tenant membership is told it has no access after the code verifies", async ({ browser }) => {
  expect(smtpCapturePath, "VITARISH_E2E_SMTP_CAPTURE_PATH must be supplied by the two-host runner").toBeTruthy();
  const context = await browser.newContext({ extraHTTPHeaders: { "X-Forwarded-For": "198.51.100.95" } });
  const page = await context.newPage();
  let failure: unknown;
  try {
    await page.goto(`${privatePracticeOrigin}${ClinicRoutes.Login.Email}`, { waitUntil: "domcontentloaded" });
    await expect(page.getByRole("heading", { name: "Email Sign In", exact: true })).toBeVisible({ timeout: 30_000 });

    const email = `e2e-no-access-${Date.now()}@example.test`;
    await page.getByLabel("Email", { exact: true }).fill(email);
    const previousModifiedAt = existsSync(smtpCapturePath!) ? statSync(smtpCapturePath!).mtimeMs : 0;
    const [beginResponse] = await Promise.all([
      page.waitForResponse((response) => response.url().endsWith("/api/modules/authentication/login/email/begin")),
      page.getByRole("button", { name: "Send Code", exact: true }).click(),
    ]);
    expect(beginResponse.status()).toBe(200);

    const code = await readDeliveredCode(previousModifiedAt);
    await page.getByLabel("Verification Code").fill(code);
    const [completeResponse] = await Promise.all([
      page.waitForResponse((response) => response.url().endsWith("/api/modules/authentication/login/email/complete")),
      page.getByRole("button", { name: "Verify Code", exact: true }).click(),
    ]);
    expect(completeResponse.status()).toBe(403);
    await expect(completeResponse.json()).resolves.toMatchObject({ code: "no_access" });
    await expect(page.getByText("This account has no access level. Contact a platform administrator.")).toBeVisible();
  } catch (error) {
    failure = error;
  }

  try {
    await context.close();
  } catch (cleanupError) {
    if (failure) {
      throw new AggregateError([failure, cleanupError], "The no-access scenario and its browser-context cleanup both failed.");
    }
    throw cleanupError;
  }
  if (failure) {
    throw failure;
  }
});

import { expect, test, type Page } from "@playwright/test";
import { existsSync, mkdirSync, readFileSync, rmSync, statSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { Routes as ClinicRoutes } from "../../apps/private-practice-owner/src/app/routes";

const privatePracticeOrigin = process.env.VITARISH_E2E_WEB_ORIGIN ?? "http://localhost:3005";
const apiOrigin = process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://localhost:7272";
const smtpCapturePath = process.env.VITARISH_E2E_SMTP_CAPTURE_PATH;
const password = process.env.VITARISH_E2E_PASSWORD;
const subjectUsername = process.env.VITARISH_E2E_EMAIL_CHANGE_USERNAME ?? "e2e-email-change-user";

async function readDeliveredCode(previousModifiedAt: number) {
  await expect.poll(() => {
    return Boolean(smtpCapturePath && existsSync(smtpCapturePath) && statSync(smtpCapturePath).mtimeMs > previousModifiedAt);
  }).toBe(true);

  const message = readFileSync(smtpCapturePath!, "utf8");
  const code = /Your verification code is:\s*(?:<strong>)?([^<\s]+)(?:<\/strong>)?/i.exec(message)?.[1];
  expect(code).toEqual(expect.any(String));
  return code!;
}

async function signIn(page: Page, username: string) {
  for (let attempt = 0; attempt < 3; attempt += 1) {
    await page.goto(`${privatePracticeOrigin}${ClinicRoutes.Login.Password}`, { waitUntil: "domcontentloaded" });
    await page.locator('input[name="username"]').fill(username);
    await page.locator('input[name="password"]').fill(password!);
    const apiResponse = page
      .waitForResponse(
        (candidate) =>
          candidate.url().endsWith("/api/modules/authentication/login/password") &&
          candidate.request().method() === "POST" &&
          (candidate.headers()["content-type"] ?? "").includes("json"),
        { timeout: 5_000 },
      )
      .catch(() => null);
    await page.locator('button[type="submit"]').click();
    const response = await apiResponse;
    if (response) {
      return response.status();
    }
    await page.waitForLoadState("domcontentloaded");
  }
  throw new Error(`the sign-in form never reached the authentication API for ${username}`);
}

async function readOwnProfileEmail(page: Page) {
  const session = await page.evaluate(async (origin) => {
    const response = await fetch(`${origin}/api/modules/authentication/session`, {
      credentials: "include",
      headers: { "X-Vitarish-Tenant-Host": window.location.host },
    });
    return response.json() as Promise<{ user?: { userId?: string } }>;
  }, apiOrigin);
  const userId = session.user?.userId;
  expect(userId, "the authenticated session must expose the subject id").toBeTruthy();
  const profile = await page.evaluate(async ({ origin, subjectId }) => {
    const response = await fetch(`${origin}/api/modules/users/${subjectId}/profile`, {
      credentials: "include",
      headers: { "X-Vitarish-Tenant-Host": window.location.host },
    });
    return response.json() as Promise<{ email?: string }>;
  }, { origin: apiOrigin, subjectId: userId! });
  expect(profile.email).toBeTruthy();
  return profile.email!;
}

test("changing the account email moves sign-in to the new address", async ({ page }) => {
  expect(smtpCapturePath).toBeTruthy();
  expect(password, "VITARISH_E2E_PASSWORD must be supplied by the disposable E2E environment").toBeTruthy();
  const newEmail = `e2e-email-change-user-changed-${Date.now()}@example.test`;

  expect(await signIn(page, subjectUsername)).toBe(200);
  const oldEmail = await readOwnProfileEmail(page);

  await page.goto(`${privatePracticeOrigin}${ClinicRoutes.Account.Email}`, { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { name: "Change Email", exact: true })).toBeVisible({ timeout: 30_000 });
  const previousModifiedAt = existsSync(smtpCapturePath!) ? statSync(smtpCapturePath!).mtimeMs : 0;
  await page.getByLabel("New Email").fill(newEmail);
  await page.getByRole("button", { name: "Send Code", exact: true }).click();
  const code = await readDeliveredCode(previousModifiedAt);
  await expect(page.getByLabel("Verification Code")).toBeVisible({ timeout: 30_000 });
  await page.getByLabel("Verification Code").fill(code);
  await page.getByRole("button", { name: "Verify Code", exact: true }).click();
  await expect(page.getByText("Email updated successfully", { exact: true })).toBeVisible({ timeout: 30_000 });

  await page.goto(`${privatePracticeOrigin}${ClinicRoutes.Logout}`, { waitUntil: "domcontentloaded" });
  await page.getByRole("button", { name: "Sign Out", exact: true }).click();
  await expect(page).toHaveURL(/\/login/, { timeout: 30_000 });

  expect(await signIn(page, oldEmail)).toBe(401);
  expect(await signIn(page, newEmail)).toBe(200);
  expect(await readOwnProfileEmail(page)).toBe(newEmail);

  await page.goto(`${privatePracticeOrigin}${ClinicRoutes.Account.ContactConfirm}`, { waitUntil: "domcontentloaded" });
  await expect(page.getByRole("heading", { name: "Confirm your contact details", exact: true })).toBeVisible({ timeout: 30_000 });

  const screenshotPath = resolve("artifacts/two-host-e2e/email-change-sign-in.png");
  rmSync(screenshotPath, { force: true });
  mkdirSync(dirname(screenshotPath), { recursive: true });
  await page.screenshot({ path: screenshotPath, fullPage: true });
  expect(existsSync(screenshotPath)).toBe(true);
});

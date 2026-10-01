import { execFileSync } from "node:child_process";
import { existsSync, mkdirSync, readFileSync, statSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { expect, test, type Page } from "@playwright/test";
import { Routes as ClinicRoutes } from "../../apps/private-practice-owner/src/app/routes";
import { Routes as PlatformRoutes } from "../../apps/platform-admin/src/app/routes";

const privatePracticeOrigin = process.env.VITARISH_E2E_WEB_ORIGIN ?? "http://localhost:3005";
const platformAdminOrigin = process.env.PLAYWRIGHT_PLATFORM_ADMIN_ORIGIN ?? "http://localhost:3006";
const smtpCapturePath = process.env.VITARISH_E2E_SMTP_CAPTURE_PATH;
const proofDirectory = resolve("artifacts/two-host-e2e/password-recovery");
const proofScreenshot = process.env.VITARISH_E2E_PROOF_SCREENSHOT;

function saveScreenshot(page: Page, name: string) {
  const path = join(proofDirectory, name);
  mkdirSync(dirname(path), { recursive: true });
  return page.screenshot({ path, fullPage: true });
}

async function readDeliveredCode(previousModifiedAt: number) {
  await expect.poll(() => {
    return Boolean(smtpCapturePath && existsSync(smtpCapturePath) && statSync(smtpCapturePath).mtimeMs > previousModifiedAt);
  }).toBe(true);

  const message = readFileSync(smtpCapturePath!, "utf8");
  const code = /Your verification code is:\s*(?:<strong>)?([^<\s]+)(?:<\/strong>)?/i.exec(message)?.[1];
  expect(code).toEqual(expect.any(String));
  return code!;
}

test("password recovery completes through both shared LoginForm hosts", async ({ page }) => {
  expect(smtpCapturePath).toBeTruthy();
  const hostFlows = [
    {
      name: "private-practice",
      origin: privatePracticeOrigin,
      loginPath: ClinicRoutes.Login.Password,
      recoveryPath: ClinicRoutes.Login.ForgotPassword,
    },
    {
      name: "platform-admin",
      origin: platformAdminOrigin,
      loginPath: PlatformRoutes.LoginPassword,
      recoveryPath: PlatformRoutes.LoginForgotPassword,
    },
  ];

  for (const host of hostFlows) {
    await page.goto(`${host.origin}${host.loginPath}`);
    await expect(page.getByRole("link", { name: "Forgot password?" })).toBeVisible();
    await saveScreenshot(page, `${host.name}-01-login.png`);
    await page.getByRole("link", { name: "Forgot password?" }).click();
    await expect(page.getByRole("heading", { name: "Reset your password" })).toBeVisible();
    await saveScreenshot(page, `${host.name}-02-recovery-start.png`);
    await expect(page.getByRole("button", { name: "Email", exact: true })).toHaveAttribute("aria-pressed", "true");
    await saveScreenshot(page, `${host.name}-03-email-channel-selected.png`);

    await page.getByLabel("Email address").fill("e2e-owner@example.test");
    await saveScreenshot(page, `${host.name}-04-contact-entered.png`);
    const previousModifiedAt = smtpCapturePath && existsSync(smtpCapturePath)
      ? statSync(smtpCapturePath).mtimeMs
      : 0;
    await page.getByRole("button", { name: "Send verification code" }).click();
    await expect(page.getByRole("status")).toContainText("If an eligible account exists");
    await saveScreenshot(page, `${host.name}-05-code-requested.png`);
    const code = await readDeliveredCode(previousModifiedAt);

    await page.getByLabel("Verification code").fill(code);
    await page.getByLabel("New password", { exact: true }).fill("Recovery-E2E-Password-2!Aa");
    await page.getByLabel("Confirm new password", { exact: true }).fill("Recovery-E2E-Password-2!Aa");
    await page.screenshot({
      path: join(proofDirectory, `${host.name}-06-code-and-password-entered.png`),
      fullPage: true,
      mask: [page.getByLabel("Verification code")],
      maskColor: "#ffffff",
    });
    await page.getByRole("button", { name: "Reset password" }).click();
    await expect(page.getByRole("status")).toHaveText("Your password has been reset.");
    await saveScreenshot(page, `${host.name}-07-password-reset.png`);
  }

  const originalPassword = process.env.VITARISH_E2E_PASSWORD;
  if (!originalPassword) throw new Error("The E2E runner did not provide the original password for fixture cleanup.");
  const cleanupHost = hostFlows[hostFlows.length - 1];
  await page.goto(`${cleanupHost.origin}${cleanupHost.recoveryPath}`);
  await page.getByLabel("Email address").fill("e2e-owner@example.test");
  const previousModifiedAt = smtpCapturePath && existsSync(smtpCapturePath)
    ? statSync(smtpCapturePath).mtimeMs
    : 0;
  await page.getByRole("button", { name: "Send verification code" }).click();
  await expect(page.getByRole("status")).toContainText("If an eligible account exists");
  const cleanupCode = await readDeliveredCode(previousModifiedAt);
  await page.getByLabel("Verification code").fill(cleanupCode);
  await page.getByLabel("New password", { exact: true }).fill(originalPassword);
  await page.getByLabel("Confirm new password", { exact: true }).fill(originalPassword);
  await page.getByRole("button", { name: "Reset password" }).click();
  await expect(page.getByRole("status")).toHaveText("Your password has been reset.");

  if (proofScreenshot) {
    const host = hostFlows[hostFlows.length - 1];
    mkdirSync(dirname(proofScreenshot), { recursive: true });
    const recoveryProofScreenshot = proofScreenshot.replace(/\.png$/i, "-password-recovery.png");
    await page.screenshot({ path: recoveryProofScreenshot, fullPage: true });
    await page.close();
    const screenshotHelper = resolve("scripts/screenshot.ps1");
    const helperOutput = join(proofDirectory, "password-recovery-screenshot-helper.png");
    execFileSync("powershell.exe", [
      "-NoProfile",
      "-ExecutionPolicy",
      "Bypass",
      "-File",
      screenshotHelper,
      "-Url",
      `${host.origin}${host.recoveryPath}`,
      "-OutputPath",
      helperOutput,
      "-WaitMilliseconds",
      "300",
    ], { stdio: "pipe" });
  }
});

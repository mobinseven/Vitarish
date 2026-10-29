import { execFileSync } from "node:child_process";
import { existsSync, mkdirSync, readFileSync, rmSync, statSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { expect, test, type Page } from "@playwright/test";
import { Routes as ClinicRoutes } from "../../apps/private-practice-owner/src/app/routes";
import { Routes as PlatformRoutes } from "../../apps/platform-admin/src/app/routes";
import { twoHostE2eSignInOrigin } from "../../scripts/two-host-e2e-artifacts.mjs";
import { authenticationPasswordRecoveryBegin } from "../../shared/auth/src/api/generated/authentication/authentication";
import { forwardedForHeaderName } from "./signInHost";

const platformAdminOrigin = process.env.PLAYWRIGHT_PLATFORM_ADMIN_ORIGIN ?? "http://localhost:3006";
const smtpCapturePath = process.env.VITARISH_E2E_SMTP_CAPTURE_PATH;
const proofDirectory = resolve("artifacts/two-host-e2e/password-recovery");
const proofScreenshot = process.env.VITARISH_E2E_PROOF_SCREENSHOT;
const ownerEmail = "e2e-owner@example.test";

// The recovery-begin route string is owned by the generated client; derive it
// from that client instead of repeating the contract literal here.
function recoveryBeginPath(): string {
  const match = /url:\s*[`"']([^`"']+)[`"']/.exec(authenticationPasswordRecoveryBegin.toString());
  if (!match) throw new Error("The generated authentication client no longer exposes the recovery-begin URL.");
  return match[1];
}

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

async function enterRecoveryEmail(page: Page) {
  // The send action stays disabled until the form hydrates; filling the
  // controlled input earlier would be discarded by hydration.
  await expect(page.getByRole("button", { name: "Email", exact: true })).toHaveAttribute("aria-pressed", "true");
  await expect(page.getByRole("button", { name: "Send verification code" })).toBeEnabled({ timeout: 30_000 });
  await page.getByLabel("Email address").fill(ownerEmail);
  await expect(page.getByLabel("Email address")).toHaveValue(ownerEmail);
}

async function requestRecoveryCode(page: Page) {
  const [response] = await Promise.all([
    page.waitForResponse((candidate) =>
      candidate.request().method() === "POST" &&
      candidate.url().endsWith(recoveryBeginPath())),
    page.getByRole("button", { name: "Send verification code" }).click(),
  ]);
  expect(response.ok()).toBe(true);
}

test("password recovery completes through both shared LoginForm hosts", async ({ page }) => {
  expect(smtpCapturePath).toBeTruthy();
  const originalPassword = process.env.VITARISH_E2E_PASSWORD;
  if (!originalPassword) {
    throw new Error("The E2E runner did not provide the original password; refusing to start a flow that changes it.");
  }
  const hostFlows = [
    {
      name: "private-practice",
      // Clinic sign-in, and its password recovery, happen on the shared sign-in host.
      origin: twoHostE2eSignInOrigin,
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

  let passwordChanged = false;

  const restoreOriginalPassword = async () => {
    const cleanupHost = hostFlows[hostFlows.length - 1];
    // The original page may be mid-flow or closed when a failure unwinds, so
    // the restore always starts from a fresh page in the same context.
    const cleanupPage = await page.context().newPage();
    try {
      await cleanupPage.goto(`${cleanupHost.origin}${cleanupHost.recoveryPath}`);
      await expect(cleanupPage.getByRole("heading", { name: "Reset your password" })).toBeVisible({ timeout: 30_000 });
      await enterRecoveryEmail(cleanupPage);
      const previousModifiedAt = smtpCapturePath && existsSync(smtpCapturePath)
        ? statSync(smtpCapturePath).mtimeMs
        : 0;
      await requestRecoveryCode(cleanupPage);
      await expect(cleanupPage.getByRole("status")).toContainText("If an eligible account exists");
      const cleanupCode = await readDeliveredCode(previousModifiedAt);
      await cleanupPage.getByLabel("Verification code").fill(cleanupCode);
      await cleanupPage.getByLabel("New password", { exact: true }).fill(originalPassword);
      await cleanupPage.getByLabel("Confirm new password", { exact: true }).fill(originalPassword);
      await cleanupPage.getByRole("button", { name: "Reset password" }).click();
      await expect(cleanupPage.getByRole("status")).toHaveText("Your password has been reset.");
    } finally {
      await cleanupPage.close();
    }
  };

  try {
    for (const host of hostFlows) {
      await page.goto(`${host.origin}${host.loginPath}`);
      await expect(page.getByRole("link", { name: "Forgot password?" })).toBeVisible();
      await saveScreenshot(page, `${host.name}-01-login.png`);
      await page.getByRole("link", { name: "Forgot password?" }).click();
      await expect(page.getByRole("heading", { name: "Reset your password" })).toBeVisible();
      await saveScreenshot(page, `${host.name}-02-recovery-start.png`);
      await saveScreenshot(page, `${host.name}-03-email-channel-selected.png`);

      await enterRecoveryEmail(page);
      await saveScreenshot(page, `${host.name}-04-contact-entered.png`);
      const previousModifiedAt = smtpCapturePath && existsSync(smtpCapturePath)
        ? statSync(smtpCapturePath).mtimeMs
        : 0;
      await requestRecoveryCode(page);
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
      passwordChanged = true;
      await saveScreenshot(page, `${host.name}-07-password-reset.png`);
    }

    if (proofScreenshot) {
      const host = hostFlows[hostFlows.length - 1];
      mkdirSync(dirname(proofScreenshot), { recursive: true });
      const recoveryProofScreenshot = proofScreenshot.replace(/\.png$/i, "-password-recovery.png");
      await page.screenshot({ path: recoveryProofScreenshot, fullPage: true });
      await page.close();
      const screenshotHelper = resolve("scripts/screenshot.ps1");
      const helperOutput = join(proofDirectory, "password-recovery-screenshot-helper.png");
      const runScreenshotHelper = () => {
        rmSync(helperOutput, { force: true });
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
        expect(existsSync(helperOutput), `the screenshot helper did not create ${helperOutput}`).toBe(true);
      };
      try {
        runScreenshotHelper();
      } catch (firstFailure) {
        const stderr = (firstFailure as { stderr?: unknown }).stderr;
        console.error("password-recovery screenshot helper failed on the first attempt; retrying once.", stderr ?? firstFailure);
        runScreenshotHelper();
      }
    }
  } finally {
    if (passwordChanged) {
      await restoreOriginalPassword();
    }
  }
});

test("SMS recovery has the same public result for malformed and unknown phone numbers", async ({ browser }) => {
  const unknownPhone = `09${Date.now().toString().slice(-9)}`;
  const contacts = ["not-a-number", unknownPhone];
  const outcomes: Array<{ notice: string; codeEntryVisible: boolean }> = [];

  for (const contact of contacts) {
    const context = await browser.newContext();
    // One OtpBegin rate-limit partition per contact so these anonymous
    // requests cannot exhaust the partition used by the phone-login scenario.
    context.setExtraHTTPHeaders({ [forwardedForHeaderName]: `198.51.100.${202 + outcomes.length}` });
    const page = await context.newPage();
    try {
      await page.goto(`${twoHostE2eSignInOrigin}${ClinicRoutes.Login.ForgotPassword}`, { waitUntil: "domcontentloaded" });
      await expect(page.getByRole("heading", { name: "Reset your password" })).toBeVisible({ timeout: 30_000 });
      await page.getByRole("button", { name: "Text message", exact: true }).click();
      await page.getByLabel("Phone number").fill(contact);
      await page.getByRole("button", { name: "Send verification code" }).click();

      const notice = page.getByRole("status");
      await expect(notice).toHaveText("If an eligible account exists, a verification code has been sent.");
      const codeEntry = page.getByLabel("Verification code");
      await expect(codeEntry).toBeVisible();
      outcomes.push({ notice: await notice.innerText(), codeEntryVisible: await codeEntry.isVisible() });
    } finally {
      await context.close();
    }
  }

  expect(outcomes[0]).toEqual(outcomes[1]);
});

import { expect, test, type Page } from "@playwright/test";
import path from "node:path";
import { Routes } from "../../apps/private-practice-owner/src/app/routes";
import { authMessageCatalogs } from "../../shared/auth/src/localization/catalogs";
import { localeCookieName } from "../../shared/localization/src/localeCookie";
import { supportedLocales } from "../../shared/localization/src/locales";
import { mockRejectedPassword } from "../../shared/mock-runtime/src/mockAuthHandlers";
import { mockAnonymousSessionValue, mockSessionCookieName } from "../../shared/mock-runtime/src/mockSession";

// Walks every authentication screen of the owner app against the mock API and captures each step:
// the sign-in methods, a rejected and a second-step password sign-in, the phone and email code steps,
// passkey sign-in, password recovery through to its new password, and sign-out.
// Only the ready scenario answers these requests; the other scenarios are covered by pages.spec.ts.
const scenario = process.env.VITARISH_SCREENSHOT_SCENARIO ?? "ready";
const outputRoot = process.env.VITARISH_SCREENSHOT_OUTPUT ?? "artifacts/private-practice-owner-screenshots";
const settleDelayMilliseconds = 500;
const networkIdleTimeoutMilliseconds = 15_000;
// What the flow types into the forms.
const userName = "owner";
const password = "correct-horse-battery";
const phoneNumber = "9121234567";
const email = "owner@example.test";
const code = "123456";
const newPassword = "New-password-2026";

test.describe.configure({ mode: "parallel" });

for (const locale of supportedLocales) {
  const failures = authMessageCatalogs[locale].auth.failures;

  test(`${scenario} auth-flow ${locale}`, async ({ context, page }, testInfo) => {
    test.skip(scenario !== "ready", "Only the ready scenario answers sign-in requests.");
    const origin = new URL(testInfo.project.use.baseURL ?? "").origin;
    await context.addCookies([
      { name: localeCookieName, value: locale, url: origin },
      { name: mockSessionCookieName, value: mockAnonymousSessionValue, url: origin },
    ]);
    let step = 0;
    const capture = async (name: string) => {
      await settle(page);
      step += 1;
      await page.screenshot({
        fullPage: true,
        path: path.join(outputRoot, scenario, `${locale}-${testInfo.project.name}`, "mock-auth-flow", `${String(step).padStart(2, "0")}-${name}.png`),
      });
    };
    const field = (name: string) => page.locator(`input[name="${name}"]`);
    const submit = () => page.locator('form button[type="submit"]').click();

    await test.step("sign-in methods", async () => {
      await open(page, Routes.Login.Index);
      await capture("sign-in-methods");
    });

    await test.step("password sign-in", async () => {
      await open(page, Routes.Login.Password);
      await capture("password");

      await field("username").fill(userName);
      await field("password").fill(mockRejectedPassword);
      await submit();
      await expect(page.getByText(failures.invalidCredentials)).toBeVisible();
      await capture("password-rejected");

      await field("password").fill(password);
      await submit();
      await expect(field("otp")).toBeVisible();
      await capture("password-second-step");
    });

    await test.step("phone sign-in", async () => {
      await open(page, Routes.Login.Phone);
      await capture("phone");

      await field("phone").fill(phoneNumber);
      await submit();
      await expect(field("otp")).toBeVisible();
      await capture("phone-code");
    });

    await test.step("email sign-in", async () => {
      await open(page, Routes.Login.Email);
      await capture("email");

      await field("email").fill(email);
      await submit();
      await expect(field("otp")).toBeVisible();
      await capture("email-code");
    });

    await test.step("passkey sign-in", async () => {
      await open(page, Routes.Login.Passkey);
      await capture("passkey");
    });

    await test.step("password recovery", async () => {
      await open(page, Routes.Login.ForgotPassword);
      await capture("recovery");

      await field("recovery-contact").fill(email);
      await submit();
      await expect(field("recovery-code")).toBeVisible();
      await capture("recovery-code");

      await field("recovery-code").fill(code);
      await field("new-password").fill(newPassword);
      await field("confirm-password").fill(newPassword);
      await submit();
      await expect(field("recovery-code")).toBeHidden();
      await capture("recovery-complete");
    });

    await test.step("sign-out", async () => {
      // Sign-out is offered to a signed-in user.
      await context.clearCookies({ name: mockSessionCookieName });
      await open(page, Routes.Logout);
      await capture("sign-out");
    });
  });
}

async function open(page: Page, pagePath: string) {
  await page.goto(pagePath, { waitUntil: "load" });
  await page.waitForFunction(() => navigator.serviceWorker.controller !== null);
  // The mock server runs in development mode; keep the framework's development indicator out of the captures.
  await page.addStyleTag({ content: "nextjs-portal { display: none !important; }" });
}

async function settle(page: Page) {
  await page.waitForLoadState("networkidle", { timeout: networkIdleTimeoutMilliseconds }).catch(() => undefined);
  await page.waitForTimeout(settleDelayMilliseconds);
}

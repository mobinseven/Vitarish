import { expect, test, type Browser, type Page } from "@playwright/test";
import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { Routes as OwnerRoutes } from "../../apps/private-practice-owner/src/app/routes";
import { Routes } from "../../apps/platform-admin/src/app/routes";
import { catalogs } from "../../apps/private-practice-owner/src/localization/messages";
import {
  authenticationLoginPassword,
  getAuthenticationSessionGetQueryKey,
} from "../../shared/auth/src/api/generated/authentication/authentication";
import { localeCookieName } from "../../shared/localization/src/localeCookie";
import { defaultLocale, toSupportedLocale } from "../../shared/localization/src/locales";
import {
  TENANT_HOST_HEADER,
} from "../../shared/api-client/src/apiFetch";
import {
  twoHostE2eForwardedForHeaderName,
  twoHostE2eLocaleEnvironmentKey,
  twoHostE2ePasswordEnvironmentKey,
  twoHostE2ePlatformAdminClientIp,
  twoHostE2ePlatformAdminOrigin,
  twoHostE2eSignInOrigin,
} from "../../scripts/two-host-e2e-artifacts.mjs";
import { generatedOperationPath } from "./generatedOperationPath";

function sourceString(path: string, name: string): string {
  const source = readFileSync(resolve(process.cwd(), path), "utf8");
  const value = new RegExp(`public const string ${name} = "([^"]+)";`).exec(source)?.[1];
  if (!value) throw new Error(`Source constant ${name} was not found in ${path}.`);
  return value;
}

async function newPlatformAdminPage(browser: Browser, locale: string): Promise<Page> {
  const context = await browser.newContext({
    extraHTTPHeaders: {
      [twoHostE2eForwardedForHeaderName]: twoHostE2ePlatformAdminClientIp,
    },
  });
  await context.addCookies([
    {
      name: localeCookieName,
      value: locale,
      url: twoHostE2ePlatformAdminOrigin,
    },
  ]);
  await context.addCookies([{ name: localeCookieName, value: locale, url: twoHostE2eSignInOrigin }]);
  return context.newPage();
}

async function getSessionStatus(page: Page): Promise<number> {
  return page.evaluate(async ({ path, tenantHostHeader }) => {
    const response = await fetch(path, {
      credentials: "include",
      headers: { [tenantHostHeader]: window.location.host },
    });
    return response.status;
  }, {
    path: getAuthenticationSessionGetQueryKey()[0],
    tenantHostHeader: TENANT_HOST_HEADER,
  });
}

const fixtureSource = "../E2e/Infrastructure/Fixtures/E2eFixtureSeeder.cs";
const platformAdminUserName = sourceString(fixtureSource, "PlatformAdminUserName");
const platformOrigin = twoHostE2ePlatformAdminOrigin;
const loginPath = generatedOperationPath(authenticationLoginPassword);
const locale = toSupportedLocale(process.env[twoHostE2eLocaleEnvironmentKey]) ?? defaultLocale;
const messages = catalogs[locale];
const password = process.env[twoHostE2ePasswordEnvironmentKey];

test(`platform administrator reviews the My account sign-in settings ${locale}`, async ({ browser }, testInfo) => {
  expect(password, "The disposable host supplies the fixture password.").toBeTruthy();

  const page = await newPlatformAdminPage(browser, locale);
  try {
    await page.goto(`${platformOrigin}${Routes.LoginPassword}`, {
      waitUntil: "domcontentloaded",
    });
    await page.locator('input[name="username"]').fill(platformAdminUserName);
    await page.locator('input[name="password"]').fill(password!);
    const [loginResponse] = await Promise.all([
      page.waitForResponse((response) =>
        response.url().endsWith(loginPath) && response.request().method() === "POST",
      ),
      page.locator('button[type="submit"]').click(),
    ]);
    expect(loginResponse.status()).toBe(200);

    await page.waitForURL(url => url.origin === platformOrigin && !url.pathname.startsWith(Routes.Login));
    await page.goto(`${platformOrigin}${Routes.Settings.MyAccount}`, {
      waitUntil: "domcontentloaded",
    });
    await page.waitForURL(twoHostE2eSignInOrigin + OwnerRoutes.Account.Index);
    await expect.poll(() => getSessionStatus(page)).toBe(200);
    await page.goto(twoHostE2eSignInOrigin + OwnerRoutes.Account.Password);
    await expect(
      page.getByLabel(messages.account.password.currentPassword, { exact: true }),
    ).toBeVisible();
    await expect(
      page.getByRole("region", {
        name: messages.account.signInSecurity.title,
        exact: true,
      }),
    ).toContainText(
      `${messages.account.signInSecurity.channel}: ${messages.account.signInSecurity.channelEmail}`,
    );
    await expect(
      page.getByRole("switch", {
        name: messages.account.signInSecurity.toggleLabel,
        exact: true,
      }),
    ).not.toBeChecked();

    await page.screenshot({
      path: testInfo.outputPath("platform-admin-my-account.png"),
      fullPage: true,
      animations: "disabled",
    });
  } finally {
    await page.context().close();
  }
});

import { expect, test } from "@playwright/test";
import path from "node:path";
import { Routes } from "../../apps/private-practice-owner/src/app/routes";
import { catalogs } from "../../apps/private-practice-owner/src/localization/messages";
import { mockScreenshotOutputEnvironmentKey, mockScreenshotRootFor, mockScreenshotScenarioEnvironmentKey, mockScenarios, defaultMockApp } from "../../scripts/spec-capture-options.mjs";
import { localeCookieName } from "../../shared/localization/src/localeCookie";
import { getLocaleDirection, supportedLocales } from "../../shared/localization/src/locales";
import { mockAnonymousSessionValue, mockSessionCookieName } from "../../shared/mock-runtime/src/mockSession";

const readyScenario = mockScenarios[0];
const scenario = process.env[mockScreenshotScenarioEnvironmentKey] ?? readyScenario;
const outputRoot = process.env[mockScreenshotOutputEnvironmentKey] ?? mockScreenshotRootFor(defaultMockApp);

for (const locale of supportedLocales) {
  test(`${scenario} ${locale} Darman editorial reading`, async ({ context, page }, testInfo) => {
    test.skip(scenario !== readyScenario, "The public article does not depend on mock data.");
    const text = catalogs[locale].practiceLanding;
    const origin = new URL(testInfo.project.use.baseURL ?? "").origin;
    await context.addCookies([
      { name: localeCookieName, value: locale, url: origin },
      { name: mockSessionCookieName, value: mockAnonymousSessionValue, url: origin },
    ]);
    await page.goto(Routes.Root, { waitUntil: "load" });
    await page.waitForFunction(() => navigator.serviceWorker.controller !== null);
    await page.getByRole("link", { name: text.editorial.readMore }).click();
    await expect(page).toHaveURL(new URL(Routes.PracticeOverview, origin).toString(), { timeout: 120_000 });
    await expect(page).toHaveTitle(text.editorial.title);
    await expect(page.locator("html")).toHaveAttribute("dir", getLocaleDirection(locale));
    await expect(page.getByRole("heading", { level: 1, name: text.editorial.title })).toBeVisible();
    await expect(page.getByText(text.featureBookingsDescription)).toBeVisible();

    const header = page.getByRole("banner");
    const headerNavigation = header.getByRole("navigation", { name: text.editorial.navigation });
    const directHome = headerNavigation.getByRole("link", { name: text.brandHome });
    const desktopNavigation = await directHome.isVisible();
    const menuButton = header.getByRole("button", { name: text.editorial.navigation });
    await expect(menuButton).toHaveAttribute("title", text.editorial.navigationTooltip);
    await menuButton.focus();
    await page.keyboard.press("Enter");
    const menu = page.getByRole("menu", { name: text.editorial.navigation });
    await expect(menu.getByRole("menuitem", { name: desktopNavigation ? text.signIn : text.brandHome })).toBeVisible();
    await page.keyboard.press("Escape");
    await expect(menu).toBeHidden();
    await expect(menuButton).toBeFocused();
    await menuButton.evaluate((element) => element.blur());
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true);
    await page.addStyleTag({ content: "nextjs-portal { display: none !important; }" });
    await page.screenshot({
      fullPage: true,
      path: path.join(outputRoot, scenario, `${locale}-${testInfo.project.name}`, "mock-editorial", "01-article.png"),
    });
    const footer = page.getByRole("contentinfo");
    const footerNavigation = footer.getByRole("navigation", { name: text.editorial.navigation });
    const footerToggle = footerNavigation.getByRole("button", { name: text.editorial.navigation });
    await footerToggle.focus();
    await page.keyboard.press("Enter");
    await expect(footerToggle).toHaveAttribute("aria-expanded", "true");
    const footerHome = footerNavigation.getByRole("link", { name: text.brandHome });
    await expect(footerHome).toBeVisible();
    await expect(footerHome).toHaveAttribute("href", Routes.Root);
    await footerToggle.evaluate((element) => element.blur());
    await footer.screenshot({
      path: path.join(outputRoot, scenario, `${locale}-${testInfo.project.name}`, "mock-editorial", "02-footer-expanded.png"),
    });
    await footerToggle.focus();
    await page.keyboard.press("Enter");
    await expect(footerToggle).toHaveAttribute("aria-expanded", "false");
    await expect(footerHome).toBeHidden();
    const otherLocale = supportedLocales.find((candidate) => candidate !== locale)!;
    const otherText = catalogs[otherLocale].practiceLanding;
    await page.getByRole("button", { name: text.otherLanguage }).click();
    await expect(page.getByRole("heading", { level: 1, name: otherText.editorial.title })).toBeVisible();
    await expect(page).toHaveTitle(otherText.editorial.title);
    const otherHeader = page.getByRole("banner");
    if (desktopNavigation) {
      await otherHeader.getByRole("navigation", { name: otherText.editorial.navigation }).getByRole("link", { name: otherText.brandHome }).click();
    } else {
      await otherHeader.getByRole("button", { name: otherText.editorial.navigation }).click();
      await page.getByRole("menu", { name: otherText.editorial.navigation }).getByRole("menuitem", { name: otherText.brandHome }).click();
    }
    await expect(page).toHaveURL(new URL(Routes.Root, origin).toString());
  });
}

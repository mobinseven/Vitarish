import { expect, test } from "@playwright/test";
import path from "node:path";
import { LandingRoutes } from "../../apps/vitarish-landing/src/app/routes";
import { splashAttribute } from "../../apps/vitarish-landing/src/components/splash/splash-attributes";
import { catalogs } from "../../apps/vitarish-landing/src/localization/messages";
import {
  landingMockApp,
  mockScreenshotOutputEnvironmentKey,
  mockScreenshotRootFor,
  mockScreenshotScenarioEnvironmentKey,
  mockScenarios,
} from "../../scripts/spec-capture-options.mjs";
import { localeCookieName } from "../../shared/localization/src/localeCookie";
import { getLocaleDirection, supportedLocales } from "../../shared/localization/src/locales";

const readyScenario = mockScenarios[0];
const scenario = process.env[mockScreenshotScenarioEnvironmentKey] ?? readyScenario;
const outputRoot = process.env[mockScreenshotOutputEnvironmentKey] ?? mockScreenshotRootFor(landingMockApp);
const frontDoorPath = LandingRoutes.Home;
const settleDelayMilliseconds = 500;
const splashSelector = `[${splashAttribute}]`;

test.describe.configure({ mode: "parallel" });

for (const locale of supportedLocales) {
  test(`${scenario} ${locale} front-door`, async ({ context, page }, testInfo) => {
    // The front door asks for no data, so every mock scenario renders the same page.
    test.skip(scenario !== readyScenario, "The front door renders the same in every mock scenario.");
    const origin = new URL(testInfo.project.use.baseURL ?? "").origin;
    await context.addCookies([{ name: localeCookieName, value: locale, url: origin }]);

    await page.goto(frontDoorPath, { waitUntil: "load" });
    await page.waitForFunction(() => navigator.serviceWorker.controller !== null);
    await page.waitForLoadState("networkidle");
    // The splash screen covers the page until the document has loaded; the screenshot is of the page behind it.
    await page.locator(splashSelector).waitFor({ state: "detached" });
    // The mock server runs in development mode; keep the framework's development indicator out of the captures.
    await page.addStyleTag({ content: "nextjs-portal { display: none !important; }" });
    await page.waitForTimeout(settleDelayMilliseconds);

    await page.screenshot({
      fullPage: true,
      path: path.join(outputRoot, scenario, `${locale}-${testInfo.project.name}`, "mock-landing", "01-front-door.png"),
    });
  });
}

for (const locale of supportedLocales) {
  test(`${scenario} ${locale} purpose reading`, async ({ context, page }, testInfo) => {
    test.skip(scenario !== readyScenario, "The public article does not depend on mock data.");
    const text = catalogs[locale].landing;
    const origin = new URL(testInfo.project.use.baseURL ?? "").origin;
    await context.addCookies([{ name: localeCookieName, value: locale, url: origin }]);
    await page.goto(LandingRoutes.Home, { waitUntil: "load" });
    await page.waitForFunction(() => navigator.serviceWorker.controller !== null);
    await page.locator(splashSelector).waitFor({ state: "detached" });
    await page.getByRole("main").getByRole("link", { name: text.purpose.readMore }).click();
    await expect(page).toHaveURL(new URL(LandingRoutes.Purpose, origin).toString(), { timeout: 60_000 });
    await page.waitForLoadState("networkidle");
    await page.locator(splashSelector).waitFor({ state: "detached" });
    await expect(page).toHaveTitle(text.purpose.title);
    await expect(page.locator("html")).toHaveAttribute("dir", getLocaleDirection(locale));
    await expect(page.getByRole("heading", { level: 1, name: text.purpose.title })).toBeVisible();
    await expect(page.getByText(text.purpose.description)).toBeVisible();

    const header = page.getByRole("banner");
    const headerNavigation = header.getByRole("navigation", { name: text.editorial.navigation });
    const directHome = headerNavigation.getByRole("link", { name: text.editorial.home });
    const desktopNavigation = await directHome.isVisible();
    const menuButton = header.getByRole("button", { name: text.editorial.navigation });
    const brand = header.getByRole("link", { name: text.editorial.home }).last();
    const languageButton = header.getByRole("button", { name: text.header.language });
    await expect(menuButton).toHaveAttribute("title", text.editorial.navigationTooltip);
    const [menuBounds, brandBounds, languageBounds] = await Promise.all([
      menuButton.boundingBox(), brand.boundingBox(), languageButton.boundingBox(),
    ]);
    expect(menuBounds).not.toBeNull();
    expect(brandBounds).not.toBeNull();
    expect(languageBounds).not.toBeNull();
    expect(overlaps(menuBounds!, brandBounds!)).toBe(false);
    expect(overlaps(brandBounds!, languageBounds!)).toBe(false);
    const menu = page.getByRole("menu", { name: text.editorial.navigation });
    // A real click waits until the splash no longer covers the article.
    await menuButton.click();
    await expect(menu.getByRole("menuitem", { name: desktopNavigation ? text.apps.practice.discover : text.editorial.home })).toBeVisible();
    await page.keyboard.press("Escape");
    await expect(menu).toBeHidden();
    await menuButton.focus();
    await page.keyboard.press("Enter");
    await expect(menu.getByRole("menuitem", { name: desktopNavigation ? text.apps.practice.discover : text.editorial.home })).toBeVisible();
    await page.keyboard.press("Escape");
    await expect(menu).toBeHidden();
    await expect(menuButton).toBeFocused();
    await menuButton.evaluate((element) => element.blur());
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true);
    await page.addStyleTag({ content: "nextjs-portal { display: none !important; }" });
    await page.screenshot({
      fullPage: true,
      path: path.join(outputRoot, scenario, `${locale}-${testInfo.project.name}`, "mock-landing", "02-purpose-article.png"),
    });
    const footer = page.getByRole("contentinfo");
    const footerNavigation = footer.getByRole("navigation", { name: text.editorial.navigation });
    const footerToggle = footerNavigation.getByRole("button", { name: text.editorial.navigation });
    await footerToggle.focus();
    await page.keyboard.press("Enter");
    await expect(footerToggle).toHaveAttribute("aria-expanded", "true");
    const footerHome = footerNavigation.getByRole("link", { name: text.editorial.home });
    await expect(footerHome).toBeVisible();
    await expect(footerHome).toHaveAttribute("href", LandingRoutes.Home);
    await footerToggle.evaluate((element) => element.blur());
    await footer.screenshot({
      path: path.join(outputRoot, scenario, `${locale}-${testInfo.project.name}`, "mock-landing", "03-purpose-footer-expanded.png"),
    });
    await footerToggle.focus();
    await page.keyboard.press("Enter");
    await expect(footerToggle).toHaveAttribute("aria-expanded", "false");
    await expect(footerHome).toBeHidden();
    const otherLocale = supportedLocales.find((candidate) => candidate !== locale)!;
    const otherText = catalogs[otherLocale].landing;
    await page.getByRole("button", { name: text.header.language }).click();
    await page.waitForLoadState("networkidle");
    await page.locator(splashSelector).waitFor({ state: "detached" });
    await expect(page.getByRole("heading", { level: 1, name: otherText.purpose.title })).toBeVisible();
    await expect(page).toHaveTitle(otherText.purpose.title);
    const otherHeader = page.getByRole("banner");
    if (desktopNavigation) {
      await otherHeader.getByRole("navigation", { name: otherText.editorial.navigation }).getByRole("link", { name: otherText.editorial.home }).click();
    } else {
      await otherHeader.getByRole("button", { name: otherText.editorial.navigation }).click();
      await page.getByRole("menu", { name: otherText.editorial.navigation }).getByRole("menuitem", { name: otherText.editorial.home }).click();
    }
    await expect(page).toHaveURL(new URL(LandingRoutes.Home, origin).toString());
  });
}

function overlaps(a: { x: number; y: number; width: number; height: number }, b: { x: number; y: number; width: number; height: number }) {
  return a.x < b.x + b.width && a.x + a.width > b.x && a.y < b.y + b.height && a.y + a.height > b.y;
}

for (const locale of supportedLocales) {
  test(`${scenario} ${locale} front-door-loading`, async ({ context, page }, testInfo) => {
    test.skip(scenario !== readyScenario, "The front door renders the same in every mock scenario.");
    const origin = new URL(testInfo.project.use.baseURL ?? "").origin;
    await context.addCookies([{ name: localeCookieName, value: locale, url: origin }]);
    // The captures run with reduced motion; the load screen's sweep only shows without it, so record it with motion on.
    await page.emulateMedia({ reducedMotion: "no-preference" });

    await page.goto(frontDoorPath, { waitUntil: "commit" });
    const loadScreen = page.locator(splashSelector);
    await loadScreen.waitFor({ state: "visible" });
    await loadScreen.waitFor({ state: "detached" });
    await page.waitForTimeout(settleDelayMilliseconds);
  });
}

import { expect, test, type Page } from "@playwright/test";
import { Routes } from "../../apps/private-practice-owner/src/app/routes";
import { themeModeAttribute } from "../../shared/ui/src/themePaletteClass";
import { themePaletteClasses } from "../../apps/private-practice-owner/src/app/theme";
import {
  themeModeOptions,
  themePreferenceStorageKey,
} from "../../apps/private-practice-owner/src/components/providers/themeMode";

type ThemeMode = (typeof themeModeOptions)[number];
type ThemePreference = ThemeMode | "system";

const themeModePath = Routes.Login.Index;
const clientChunks = "**/_next/static/chunks/**";

const scenarios: { preference: ThemePreference; os: ThemeMode; expected: ThemeMode }[] = [
  { preference: "light", os: "light", expected: "light" },
  { preference: "dark", os: "light", expected: "dark" },
  { preference: "light", os: "dark", expected: "light" },
  { preference: "system", os: "light", expected: "light" },
  { preference: "system", os: "dark", expected: "dark" },
];

test.describe("private practice owner theme mode", () => {
  for (const { preference, os, expected } of scenarios) {
    test(`${preference} preference on a ${os} OS resolves to the ${expected} palette before and after hydration`, async ({
      page,
    }) => {
      const hydrationErrors = collectHydrationErrors(page);
      await seedPreference(page, preference);
      await page.emulateMedia({ colorScheme: os });

      // Without client chunks only the inline head/body scripts run: this is the first-paint state.
      await page.route(clientChunks, (route) => route.abort());
      await page.goto(themeModePath, { waitUntil: "load" });
      const firstPaint = await readThemeState(page);

      await page.unroute(clientChunks);
      await page.reload({ waitUntil: "networkidle" });
      const hydrated = await readThemeState(page);

      expect(firstPaint).toEqual({
        mode: expected,
        paletteClasses: [themePaletteClasses[expected]],
        colorScheme: expected,
        background: expect.any(String),
        highlight: expect.any(String),
      });
      expect(hydrated).toEqual(firstPaint);
      expect(hydrationErrors).toEqual([]);
    });
  }

  test("light and dark resolve to different theme and app palettes", async ({ page }) => {
    await seedPreference(page, "light");
    await page.goto(themeModePath, { waitUntil: "networkidle" });
    const light = await readThemeState(page);

    await setPreference(page, "dark");
    await expect.poll(() => readThemeState(page).then((state) => state.mode)).toBe("dark");
    const dark = await readThemeState(page);

    expect(dark.background).not.toBe(light.background);
    expect(dark.highlight).not.toBe(light.highlight);
  });

  test("follows a preference change made in another tab without a reload", async ({ page, context }) => {
    await seedPreference(page, "dark");
    await page.emulateMedia({ colorScheme: "light" });
    await page.goto(themeModePath, { waitUntil: "networkidle" });
    expect((await readThemeState(page)).paletteClasses).toEqual([themePaletteClasses.dark]);

    const otherTab = await context.newPage();
    await otherTab.goto(themeModePath, { waitUntil: "networkidle" });
    await setPreference(otherTab, "light");

    await expect
      .poll(() => readThemeState(page))
      .toMatchObject({ mode: "light", paletteClasses: [themePaletteClasses.light], colorScheme: "light" });
  });
});

/** Seeds the stored preference once per context, so later changes in the test are not overwritten on navigation. */
async function seedPreference(page: Page, preference: ThemePreference) {
  await page.addInitScript(
    ([key, value]) => {
      if (localStorage.getItem(key) === null) localStorage.setItem(key, value);
    },
    [themePreferenceStorageKey, preference] as const,
  );
}

/** Writes the preference the way another tab's theme toggle does; next-themes listens for the storage event. */
async function setPreference(page: Page, preference: ThemePreference) {
  await page.evaluate(
    ([key, value]) => {
      const oldValue = localStorage.getItem(key);
      localStorage.setItem(key, value);
      window.dispatchEvent(new StorageEvent("storage", { key, oldValue, newValue: value }));
    },
    [themePreferenceStorageKey, preference] as const,
  );
}

function collectHydrationErrors(page: Page) {
  const errors: string[] = [];
  page.on("console", (message) => {
    if (message.type() === "error" && /hydrat/i.test(message.text())) errors.push(message.text());
  });
  return errors;
}

async function readThemeState(page: Page) {
  return page.evaluate(
    ([modeAttribute, paletteClasses]) => {
      const root = document.documentElement;
      const style = getComputedStyle(root);
      return {
        mode: root.getAttribute(modeAttribute),
        paletteClasses: Object.values(paletteClasses).filter((name) => root.classList.contains(name)),
        colorScheme: style.colorScheme,
        background: style.getPropertyValue("--background").trim(),
        highlight: style.getPropertyValue("--highlight").trim(),
      };
    },
    [themeModeAttribute, themePaletteClasses] as const,
  );
}

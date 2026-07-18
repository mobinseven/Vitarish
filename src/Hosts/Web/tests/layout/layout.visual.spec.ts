import { expect, test, type BrowserContext, type Page } from "@playwright/test";

interface VisualCase {
  authenticated: boolean;
  fullPage?: boolean;
  locale: "en" | "fa";
  name: string;
  readySelector: string;
  snapshot: string;
  url: string;
  viewport: { width: number; height: number };
  beforeScreenshot?: (page: Page) => Promise<void>;
}

const visualCases: readonly VisualCase[] = [
  {
    authenticated: false,
    fullPage: true,
    locale: "en",
    name: "private marketing document",
    readySelector: '[data-vitarish-layout="public-document-shell"]',
    snapshot: "private-marketing-document.png",
    url: "http://127.0.0.1:3000/",
    viewport: { width: 1440, height: 900 },
  },
  {
    authenticated: false,
    locale: "fa",
    name: "standalone centered login",
    readySelector: '[data-vitarish-layout="standalone-centered-page"]',
    snapshot: "standalone-centered-login-rtl.png",
    url: "http://127.0.0.1:3001/login",
    viewport: { width: 390, height: 844 },
  },
  {
    authenticated: true,
    locale: "fa",
    name: "contained dashboard",
    readySelector: '[data-vitarish-layout="contained-page"]',
    snapshot: "contained-dashboard-rtl.png",
    url: "http://127.0.0.1:3000/account",
    viewport: { width: 768, height: 1024 },
  },
  {
    authenticated: true,
    locale: "en",
    name: "mobile sidebar",
    readySelector: '[data-vitarish-layout="contained-page"]',
    snapshot: "mobile-sidebar-open.png",
    url: "http://127.0.0.1:3000/layout-fixture/contextual-aside",
    viewport: { width: 390, height: 844 },
    beforeScreenshot: async (page) => {
      await page.locator('[data-slot="app-layout-menu-toggle"]').click();
      await expect(page.locator('[data-slot="sidebar-mobile"]')).toBeVisible();
    },
  },
  {
    authenticated: true,
    locale: "en",
    name: "contextual aside",
    readySelector: '[data-slot="app-layout-aside"]',
    snapshot: "contextual-aside-desktop.png",
    url: "http://127.0.0.1:3000/layout-fixture/contextual-aside?aside=open",
    viewport: { width: 1440, height: 900 },
  },
  {
    authenticated: true,
    locale: "en",
    name: "dashboard workspace",
    readySelector: ".chat-conversation",
    snapshot: "dashboard-workspace.png",
    url: "http://127.0.0.1:3000/knowledge",
    viewport: { width: 1440, height: 900 },
  },
  {
    authenticated: true,
    locale: "fa",
    name: "standalone workspace",
    readySelector: ".chat-conversation",
    snapshot: "standalone-workspace-rtl.png",
    url: "http://127.0.0.1:3001/knowledge",
    viewport: { width: 390, height: 844 },
  },
];

for (const visualCase of visualCases) {
  test(`@visual ${visualCase.name}`, async ({ context, page }) => {
    await openVisualCase(context, page, visualCase);
    await visualCase.beforeScreenshot?.(page);
    await stabilizeVisuals(page, Boolean(visualCase.fullPage));

    await expect(page).toHaveScreenshot(visualCase.snapshot, {
      fullPage: visualCase.fullPage,
    });
  });
}

async function openVisualCase(
  context: BrowserContext,
  page: Page,
  visualCase: VisualCase
) {
  await context.addCookies([
    {
      domain: "127.0.0.1",
      name: "vitarish.locale",
      path: "/",
      value: visualCase.locale,
    },
    {
      domain: "127.0.0.1",
      name: "vitarish.layout-auth",
      path: "/",
      value: visualCase.authenticated ? "authenticated" : "anonymous",
    },
  ]);
  await page.setViewportSize(visualCase.viewport);
  await page.route("**/*", async (route) => {
    const url = new URL(route.request().url());
    if (url.hostname === "127.0.0.1" || url.hostname === "localhost") {
      await route.continue();
    } else {
      await route.abort("blockedbyclient");
    }
  });
  await page.goto(visualCase.url, { waitUntil: "domcontentloaded" });
  await expect(page.locator(visualCase.readySelector).first()).toBeVisible();
}

async function stabilizeVisuals(page: Page, primeDocumentImages: boolean) {
  await page.addStyleTag({
    content:
      "*, *::before, *::after { animation-duration: 0s !important; " +
      "transition-duration: 0s !important; caret-color: transparent !important; }",
  });
  await page.evaluate(() => document.fonts.ready);

  if (primeDocumentImages) {
    await page.evaluate(async () => {
      const root = document.scrollingElement!;
      for (let top = 0; top < root.scrollHeight; top += window.innerHeight) {
        window.scrollTo(0, top);
        await new Promise<void>((resolve) => requestAnimationFrame(() => resolve()));
      }
      window.scrollTo(0, 0);
    });
  }

  await page.locator("img").evaluateAll(async (images) => {
    await Promise.all(
      images.map((image) =>
        image.complete
          ? Promise.resolve()
          : new Promise<void>((resolve) => {
              image.addEventListener("load", () => resolve(), { once: true });
              image.addEventListener("error", () => resolve(), { once: true });
            })
      )
    );
  });
}

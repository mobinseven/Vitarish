import { expect, test, type BrowserContext, type Page } from "@playwright/test";

type Locale = "en" | "fa";
type ScrollModel = "document" | "frame-page" | "dashboard-workspace" | "standalone-workspace";

interface LayoutState {
  authenticated: boolean;
  expectContained?: boolean;
  expectPublicChrome?: boolean;
  name: string;
  readySelector: string;
  scrollModel: ScrollModel;
  url: string;
}

const states: readonly LayoutState[] = [
  {
    authenticated: false,
    expectPublicChrome: true,
    name: "private public document",
    readySelector: '[data-vitarish-layout="public-document-shell"]',
    scrollModel: "document",
    url: "http://127.0.0.1:3000/",
  },
  {
    authenticated: false,
    name: "demo public document",
    readySelector: '[data-vitarish-layout="public-document-shell"]',
    scrollModel: "document",
    url: "http://127.0.0.1:3001/",
  },
  {
    authenticated: false,
    name: "standalone centered login",
    readySelector: '[data-vitarish-layout="standalone-centered-page"]',
    scrollModel: "document",
    url: "http://127.0.0.1:3001/login",
  },
  {
    authenticated: true,
    expectContained: true,
    name: "contained dashboard account",
    readySelector: '[data-vitarish-layout="contained-page"]',
    scrollModel: "frame-page",
    url: "http://127.0.0.1:3000/account",
  },
  {
    authenticated: true,
    expectContained: true,
    name: "mobile sidebar dashboard",
    readySelector: '[data-vitarish-layout="contained-page"]',
    scrollModel: "frame-page",
    url: "http://127.0.0.1:3000/layout-fixture/contextual-aside",
  },
  {
    authenticated: true,
    expectContained: true,
    name: "contextual aside dashboard",
    readySelector: '[data-vitarish-layout="contained-page"]',
    scrollModel: "frame-page",
    url: "http://127.0.0.1:3000/layout-fixture/contextual-aside?aside=open",
  },
  {
    authenticated: true,
    name: "dashboard Knowledge workspace",
    readySelector: '.chat-conversation',
    scrollModel: "dashboard-workspace",
    url: "http://127.0.0.1:3000/knowledge",
  },
  {
    authenticated: true,
    name: "standalone Knowledge workspace",
    readySelector: '.chat-conversation',
    scrollModel: "standalone-workspace",
    url: "http://127.0.0.1:3001/knowledge",
  },
];

const matrixViewports = [
  { name: "mobile", width: 390, height: 844 },
  { name: "boundary-tablet", width: 768, height: 1024 },
  { name: "desktop", width: 1440, height: 900 },
] as const;

for (const state of states) {
  for (const locale of ["en", "fa"] as const) {
    for (const viewport of matrixViewports) {
      test(`${state.name} is structurally sound at ${viewport.name} in ${locale}`, async ({
        context,
        page,
      }) => {
        await openLayoutState({ context, locale, page, state, viewport });
        await assertLayoutState(page, state, locale);
      });
    }
  }
}

test("document, frame-page, and workspace models have distinct primary scroll owners", async ({
  context,
  page,
}) => {
  const mobile = { width: 390, height: 844 };

  await openLayoutState({ context, locale: "en", page, state: states[0], viewport: mobile });
  const documentProbe = await page.evaluate(() => {
    const root = document.scrollingElement!;
    const before = root.scrollTop;
    root.scrollTop = Math.min(500, root.scrollHeight - root.clientHeight);
    return {
      before,
      after: root.scrollTop,
      canScroll: root.scrollHeight > root.clientHeight,
    };
  });
  expect(documentProbe.canScroll).toBe(true);
  expect(documentProbe.after).toBeGreaterThan(documentProbe.before);

  await openLayoutState({ context, locale: "en", page, state: states[4], viewport: mobile });
  const frameProbe = await page.evaluate(() => {
    const frameMain = document.querySelector<HTMLElement>('[data-slot="app-layout-main"]')!;
    frameMain.scrollTop = Math.min(500, frameMain.scrollHeight - frameMain.clientHeight);
    return {
      documentScrollTop: document.scrollingElement!.scrollTop,
      frameScrollTop: frameMain.scrollTop,
      canScroll: frameMain.scrollHeight > frameMain.clientHeight,
    };
  });
  expect(frameProbe.canScroll).toBe(true);
  expect(frameProbe.frameScrollTop).toBeGreaterThan(0);
  expect(frameProbe.documentScrollTop).toBe(0);

  await openLayoutState({ context, locale: "en", page, state: states[6], viewport: mobile });
  const textbox = page.getByRole("textbox");
  const fixtureQuestion = "How does the deterministic layout fixture behave?";
  await textbox.fill(fixtureQuestion);
  await expect(textbox).toHaveValue(fixtureQuestion);
  const askButton = page.getByRole("button", { name: "Ask", exact: true });
  await expect(askButton).toBeEnabled();
  await askButton.click();
  await expect(page.getByText(/deterministic fixture answer/i).first()).toBeVisible();

  const workspaceProbe = await page.locator(".chat-conversation").evaluate((scroller) => {
    scroller.scrollTop = Math.min(500, scroller.scrollHeight - scroller.clientHeight);
    const frameMain = document.querySelector<HTMLElement>('[data-slot="app-layout-main"]')!;
    return {
      documentScrollTop: document.scrollingElement!.scrollTop,
      frameMainScrollTop: frameMain.scrollTop,
      workspaceScrollTop: scroller.scrollTop,
      workspaceCanScroll: scroller.scrollHeight > scroller.clientHeight,
    };
  });
  expect(workspaceProbe.workspaceCanScroll).toBe(true);
  expect(workspaceProbe.workspaceScrollTop).toBeGreaterThan(0);
  expect(workspaceProbe.frameMainScrollTop).toBe(0);
  expect(workspaceProbe.documentScrollTop).toBe(0);
});

test("dashboard skip link is the first keyboard stop and focuses the stable main target", async ({
  context,
  page,
}) => {
  await openLayoutState({
    context,
    locale: "en",
    page,
    state: states[4],
    viewport: { width: 1440, height: 900 },
  });

  const skipLink = page.locator(".vitarish-skip-link");
  await page.keyboard.press("Tab");
  await expect(skipLink).toBeFocused();
  await page.keyboard.press("Enter");
  await expect(page.locator("#vitarish-dashboard-main")).toBeFocused();
});

for (const width of [767, 768, 769]) {
  test(`native sidebar transition is correct at ${width}px`, async ({ context, page }) => {
    await openLayoutState({
      context,
      locale: "en",
      page,
      state: states[4],
      viewport: { width, height: 900 },
    });

    const menuToggle = page.locator('[data-slot="app-layout-menu-toggle"]');
    const desktopSidebar = page.locator('[data-slot="sidebar"]');
    if (width <= 768) {
      await expect(menuToggle).toBeVisible();
      await expect(desktopSidebar).toBeHidden();
      await menuToggle.click();
      await expect(page.locator('[data-slot="sidebar-mobile"]')).toBeVisible();
    } else {
      await expect(menuToggle).toBeHidden();
      await expect(desktopSidebar).toBeVisible();
      await expect(page.locator('[data-slot="sidebar-mobile"]')).toHaveCount(0);
    }
  });
}

for (const width of [1023, 1024, 1025]) {
  test(`native contextual aside transition is correct at ${width}px`, async ({
    context,
    page,
  }) => {
    await openLayoutState({
      context,
      locale: "en",
      page,
      state: states[5],
      viewport: { width, height: 900 },
    });

    const desktopAside = page.locator('[data-slot="app-layout-aside"]');
    const mobileAside = page.locator('[data-slot="app-layout-mobile-aside"]');
    if (width <= 1024) {
      await expect(desktopAside).toBeHidden();
      await expect(mobileAside).toBeVisible();
      await expect(page.locator('[data-slot="app-layout-aside-trigger"]')).toBeHidden();
    } else {
      await expect(desktopAside).toBeVisible();
      await expect(mobileAside).toHaveCount(0);
      await expect(page.locator('[data-slot="app-layout-aside-trigger"]')).toBeVisible();
    }
  });
}

test("320px enlarged-text reflow has one main and no horizontal overflow", async ({
  context,
  page,
}) => {
  await openLayoutState({
    context,
    locale: "fa",
    page,
    state: states[0],
    viewport: { width: 320, height: 800 },
  });
  await page.addStyleTag({ content: ":root { font-size: 200% !important; }" });

  await expect(page.locator("main")).toHaveCount(1);
  await assertNoHorizontalOverflow(page);
  await expect(page.locator("html")).toHaveAttribute("dir", "rtl");
});

async function openLayoutState({
  context,
  locale,
  page,
  state,
  viewport,
}: {
  context: BrowserContext;
  locale: Locale;
  page: Page;
  state: LayoutState;
  viewport: { width: number; height: number };
}) {
  await context.clearCookies();
  await context.addCookies([
    { domain: "127.0.0.1", name: "vitarish.locale", path: "/", value: locale },
    {
      domain: "127.0.0.1",
      name: "vitarish.layout-auth",
      path: "/",
      value: state.authenticated ? "authenticated" : "anonymous",
    },
  ]);
  await page.setViewportSize(viewport);
  await blockExternalRequests(page);
  await page.goto(state.url, { waitUntil: "domcontentloaded" });
  await expect(page.locator(state.readySelector).first()).toBeVisible();
  await page.evaluate(() => document.fonts.ready);
}

async function assertLayoutState(page: Page, state: LayoutState, locale: Locale) {
  await expect(page.locator("main")).toHaveCount(1);
  await expect(page.locator("html")).toHaveAttribute("lang", locale);
  await expect(page.locator("html")).toHaveAttribute("dir", locale === "fa" ? "rtl" : "ltr");
  await assertNoHorizontalOverflow(page);

  if (state.scrollModel === "document") {
    await expect(page.locator("[data-app-layout]")).toHaveCount(0);
    const documentGeometry = await page.evaluate(() => {
      const main = document.querySelector("main")!;
      const shell = document.querySelector<HTMLElement>(
        '[data-vitarish-layout="public-document-shell"]'
      );
      return {
        mainOverflowY: getComputedStyle(main).overflowY,
        shellDisplay: shell ? getComputedStyle(shell).display : null,
        shellHeight: shell?.getBoundingClientRect().height ?? null,
        viewportHeight: window.innerHeight,
      };
    });
    expect(["auto", "scroll", "hidden"]).not.toContain(documentGeometry.mainOverflowY);
    if (documentGeometry.shellHeight !== null) {
      expect(documentGeometry.shellDisplay).toBe("grid");
      expect(documentGeometry.shellHeight).toBeGreaterThanOrEqual(
        documentGeometry.viewportHeight - 1
      );
    }
    if (state.expectPublicChrome) {
      await expect(page.locator('[data-vitarish-layout-slot="header"]')).toBeVisible();
      await expect(page.locator('[data-vitarish-layout-slot="footer"]')).toBeVisible();
    }
    return;
  }

  if (state.scrollModel === "standalone-workspace") {
    await expect(page.locator("[data-app-layout]")).toHaveCount(0);
    const standaloneMain = page.locator(
      '[data-vitarish-layout="standalone-immersive-page"]'
    );
    await expect(standaloneMain).toBeVisible();
    await expect(standaloneMain).toHaveCSS("overflow", "hidden");
    await expect(page.locator('[data-vitarish-layout="immersive-region"]')).toBeVisible();
    await expect(page.locator(".chat-conversation")).toHaveCSS("overflow-y", "auto");

    const standaloneGeometry = await page.evaluate(() => {
      const root = document.querySelector<HTMLElement>(
        '[data-vitarish-layout="standalone-immersive-page"]'
      )!;
      const scrollingElement = document.scrollingElement!;
      return {
        documentClientHeight: scrollingElement.clientHeight,
        documentScrollHeight: scrollingElement.scrollHeight,
        rootHeight: root.getBoundingClientRect().height,
        viewportHeight: window.innerHeight,
      };
    });
    expect(Math.abs(standaloneGeometry.rootHeight - standaloneGeometry.viewportHeight)).toBeLessThanOrEqual(1);
    expect(standaloneGeometry.documentScrollHeight).toBeLessThanOrEqual(
      standaloneGeometry.documentClientHeight + 1
    );
    return;
  }

  const frame = page.locator('[data-vitarish-layout="dashboard-frame"]');
  const frameMain = page.locator('[data-slot="app-layout-main"]');
  await expect(frame).toBeVisible();
  await expect(frameMain).toHaveCount(1);
  await expect(page.locator('[data-slot="app-layout-header"]')).toHaveCSS("position", "sticky");

  const frameGeometry = await page.evaluate(() => {
    const root = document.querySelector<HTMLElement>('[data-vitarish-layout="dashboard-frame"]')!;
    const main = document.querySelector<HTMLElement>('[data-slot="app-layout-main"]')!;
    const scrollingElement = document.scrollingElement!;
    return {
      documentClientHeight: scrollingElement.clientHeight,
      documentScrollHeight: scrollingElement.scrollHeight,
      frameHeight: root.getBoundingClientRect().height,
      mainOverflowY: getComputedStyle(main).overflowY,
      viewportHeight: window.innerHeight,
    };
  });
  expect(Math.abs(frameGeometry.frameHeight - frameGeometry.viewportHeight)).toBeLessThanOrEqual(1);
  expect(frameGeometry.documentScrollHeight).toBeLessThanOrEqual(
    frameGeometry.documentClientHeight + 1
  );

  if (state.scrollModel === "frame-page") {
    await expect(frame).toHaveAttribute("data-content-mode", "page");
    await expect(frameMain).toHaveAttribute("data-scroll-mode", "content");
    expect(frameGeometry.mainOverflowY).toBe("auto");
    if (state.expectContained) {
      await expect(page.locator('[data-vitarish-layout="contained-page"]')).toBeVisible();
    }
    return;
  }

  await expect(page.locator('[data-vitarish-layout="immersive-region"]')).toBeVisible();
  expect(frameGeometry.mainOverflowY).toBe("hidden");
  await expect(frame).toHaveAttribute("data-content-mode", "workspace");
  await expect(frameMain).toHaveAttribute("data-scroll-mode", "page");
  await expect(page.locator(".chat-conversation")).toHaveCSS("overflow-y", "auto");
}

async function assertNoHorizontalOverflow(page: Page) {
  const overflow = await page.evaluate(() => {
    const rootScrollWidth = document.documentElement.scrollWidth;
    const offenders = Array.from(document.body.querySelectorAll<HTMLElement>("*"))
      .map((element) => {
        const rect = element.getBoundingClientRect();
        return {
          className: typeof element.className === "string" ? element.className : "",
          left: Math.round(rect.left),
          overflow: Math.round(Math.max(-rect.left, rect.right - window.innerWidth)),
          right: Math.round(rect.right),
          tagName: element.tagName.toLowerCase(),
        };
      })
      .filter(({ left, right }) => left < -1 || right > window.innerWidth + 1)
      .sort((left, right) => right.overflow - left.overflow)
      .slice(0, 12);

    return {
      bodyClientWidth: document.body.clientWidth,
      bodyScrollWidth: document.body.scrollWidth,
      rootClientWidth: document.documentElement.clientWidth,
      rootScrollWidth,
      offenders,
    };
  });
  const details = `Horizontal overflow offenders: ${JSON.stringify(overflow.offenders)}`;
  expect(overflow.rootScrollWidth, details).toBeLessThanOrEqual(overflow.rootClientWidth + 1);
  expect(overflow.bodyScrollWidth, details).toBeLessThanOrEqual(overflow.bodyClientWidth + 1);
}

async function blockExternalRequests(page: Page) {
  await page.route("**/*", async (route) => {
    const url = new URL(route.request().url());
    if (url.hostname === "127.0.0.1" || url.hostname === "localhost") {
      await route.continue();
    } else {
      await route.abort("blockedbyclient");
    }
  });
}

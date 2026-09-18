import { expect, test, type Locator } from "@playwright/test";

const viewportRevealTestPath = "/viewport-reveal-test";
const revealSelector = '[data-testid="viewport-reveal-target"]';

test.describe("ViewportReveal", () => {
  test("reveals a section after it enters the viewport", async ({ page }) => {
    await page.emulateMedia({ reducedMotion: "no-preference" });
    await page.setViewportSize({ width: 1280, height: 720 });
    await page.goto(viewportRevealTestPath, { waitUntil: "domcontentloaded" });

    const reveal = page.locator(revealSelector);
    await expect(reveal).toHaveAttribute("data-state", "hidden");
    await expect(reveal).toHaveAttribute(
      "data-vitarish-viewport-reveal-enhanced",
      "",
    );
    await expect.poll(() => readRevealStyle(reveal)).toEqual({
      opacity: "0",
      translateY: 24,
    });

    await reveal.scrollIntoViewIfNeeded();
    await expect(reveal).toHaveAttribute("data-state", "revealed");
    await expect.poll(() => readRevealStyle(reveal)).toEqual({
      opacity: "1",
      translateY: 0,
    });
  });

  test("keeps the section visible when reduced motion is requested", async ({
    page,
  }) => {
    await page.emulateMedia({ reducedMotion: "reduce" });
    await page.setViewportSize({ width: 1280, height: 720 });
    await page.goto(viewportRevealTestPath, { waitUntil: "domcontentloaded" });

    const reveal = page.locator(revealSelector);
    await expect(reveal).toHaveAttribute("data-state", "hidden");
    await expect.poll(() => readRevealStyle(reveal)).toEqual({
      opacity: "1",
      translateY: 0,
    });
  });
});

async function readRevealStyle(reveal: Locator) {
  return reveal.evaluate((element) => {
    const computedStyle = getComputedStyle(element);
    const transform = computedStyle.transform;

    return {
      opacity: computedStyle.opacity,
      translateY: transform === "none" ? 0 : new DOMMatrix(transform).m42,
    };
  });
}

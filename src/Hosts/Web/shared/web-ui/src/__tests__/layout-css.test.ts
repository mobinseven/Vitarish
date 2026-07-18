import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";

const css = readFileSync(resolve(process.cwd(), "src/layout.css"), "utf8");

describe("shared layout policy stylesheet", () => {
  it("defines canonical gutters, spacing, and widths", () => {
    expect(css).toContain("--vitarish-layout-gutter-inline: 1rem");
    expect(css).toContain("--vitarish-layout-gutter-inline: 1.5rem");
    expect(css).toContain("--vitarish-layout-gutter-inline: 2rem");
    expect(css).toContain("--vitarish-layout-page-block-start: 1.5rem");
    expect(css).toContain("--vitarish-layout-page-block-start: 2rem");
    expect(css).toContain("--vitarish-layout-page-block-end: 2.5rem");
    expect(css).toContain("--vitarish-layout-width-compact: 28rem");
    expect(css).toContain("--vitarish-layout-width-reading: 48rem");
    expect(css).toContain("--vitarish-layout-width-content: 80rem");
  });

  it("keeps viewport fallbacks in legacy-then-dynamic order", () => {
    const centeredRule = css.slice(
      css.indexOf(".vitarish-standalone-centered-page"),
      css.indexOf(".vitarish-standalone-immersive-page")
    );
    const immersiveRule = css.slice(
      css.indexOf(".vitarish-standalone-immersive-page"),
      css.indexOf('[data-vitarish-layout="dashboard-frame"]')
    );

    expect(centeredRule.indexOf("100vh")).toBeLessThan(centeredRule.indexOf("100dvh"));
    expect(immersiveRule.indexOf("100vh")).toBeLessThan(immersiveRule.indexOf("100dvh"));
  });

  it("uses physical safe-area insets with logical layout properties", () => {
    expect(css).toContain("env(safe-area-inset-left, 0px)");
    expect(css).toContain("env(safe-area-inset-right, 0px)");
    expect(css).toContain("padding-inline: var(--vitarish-layout-gutter-inline)");
    expect(css).toContain("min-inline-size: 0");
    expect(css).toContain("min-block-size: 0");
  });

  it("defines the public grid and distinct frame scroll owners", () => {
    expect(css).toContain("grid-template-rows: auto minmax(0, 1fr) auto");
    expect(css).toMatch(/data-content-mode="page"[\s\S]*?overflow-y: auto/);
    expect(css).toMatch(/data-content-mode="workspace"[\s\S]*?overflow: hidden/);
    expect(css).toContain("scrollbar-gutter: stable");
  });
});

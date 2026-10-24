import { existsSync, readFileSync, readdirSync } from "node:fs";
import { dirname, relative, resolve } from "node:path";
import { describe, expect, it } from "vitest";

const appRoot = resolve(process.cwd(), "src/app");
const pageOwnedLayoutPattern =
  /(?:Standalone(?:Centered|Document|Immersive)Page|DashboardShell|ViewportRevealTestClient|[A-Z][A-Za-z]+(?:PageWrapper|LayoutFixture)|OwnerLandingPage)/;

describe("private-practice route layout coverage", () => {
  it("gives every page an ancestor layout or explicit page-owned composition", () => {
    const uncoveredPages = collectPageFiles(appRoot).filter(
      (pagePath) =>
        !hasSegmentLayout(pagePath) &&
        !pageOwnedLayoutPattern.test(readFileSync(pagePath, "utf8")),
    );

    expect(uncoveredPages.map((pagePath) => relative(appRoot, pagePath))).toEqual([]);
  });

  it("assigns procedure-template management to the clinic workspace layout", () => {
    const pagePath = resolve(
      appRoot,
      "(owner)",
      "(clinic-workspace)",
      "settings",
      "procedure-templates",
      "page.tsx",
    );

    expect(existsSync(pagePath)).toBe(true);
    expect(existsSync(resolve(appRoot, "(owner)", "(clinic-workspace)", "layout.tsx"))).toBe(true);
  });

  it("assigns the surgery routes to their owning clinic layouts", () => {
    for (const segments of [
      ["surgeries", "page.tsx"],
      ["surgeries", "new", "page.tsx"],
      ["surgeries", "[surgeryId]", "page.tsx"],
      ["surgeries", "[surgeryId]", "edit", "page.tsx"],
    ]) {
      expect(existsSync(resolve(appRoot, "(owner)", "(clinic-page)", ...segments))).toBe(true);
    }
    expect(existsSync(resolve(appRoot, "(owner)", "(clinic-page)", "layout.tsx"))).toBe(true);

    expect(
      existsSync(
        resolve(
          appRoot,
          "(owner)",
          "(clinic-workspace)",
          "settings",
          "surgery-types",
          "page.tsx",
        ),
      ),
    ).toBe(true);
    expect(existsSync(resolve(appRoot, "(owner)", "(clinic-workspace)", "layout.tsx"))).toBe(true);
  });
});

function collectPageFiles(directory: string): string[] {
  return readdirSync(directory, { withFileTypes: true }).flatMap((entry) => {
    const path = resolve(directory, entry.name);
    if (entry.isDirectory()) return collectPageFiles(path);
    return entry.isFile() && entry.name === "page.tsx" ? [path] : [];
  });
}

function hasSegmentLayout(pagePath: string) {
  let directory = dirname(pagePath);

  while (directory !== appRoot) {
    if (existsSync(resolve(directory, "layout.tsx"))) return true;
    directory = dirname(directory);
  }

  return false;
}

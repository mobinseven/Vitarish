import { existsSync, readFileSync, readdirSync } from "node:fs";
import { dirname, relative, resolve } from "node:path";
import { describe, expect, it } from "vitest";

const appRoot = resolve(process.cwd(), "src/app");
const pageOwnedLayoutPattern =
  /(?:Standalone(?:Centered|Document|Immersive)Page|[A-Z][A-Za-z]+(?:PageWrapper|LayoutFixture))/;

describe("private-practice route layout coverage", () => {
  it("gives every page an ancestor layout or explicit page-owned composition", () => {
    const uncoveredPages = collectPageFiles(appRoot).filter(
      (pagePath) =>
        !hasSegmentLayout(pagePath) &&
        !pageOwnedLayoutPattern.test(readFileSync(pagePath, "utf8")),
    );

    expect(uncoveredPages.map((pagePath) => relative(appRoot, pagePath))).toEqual([]);
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

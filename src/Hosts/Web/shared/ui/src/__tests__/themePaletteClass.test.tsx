import { cleanup, render } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it } from "vitest";
import {
  ThemePaletteClassScript,
  brutalismThemePaletteClasses,
  themeModeAttribute,
  themePaletteClassesFor,
} from "../themePaletteClass";

const root = document.documentElement;

async function setMode(mode: string | null) {
  if (mode === null) root.removeAttribute(themeModeAttribute);
  else root.setAttribute(themeModeAttribute, mode);
  // MutationObserver callbacks run as microtasks, before the next paint.
  await Promise.resolve();
}

function runRenderedScript() {
  const { container } = render(<ThemePaletteClassScript classes={brutalismThemePaletteClasses} />);
  // The inline script must be self-contained: evaluate it without module scope.
  new Function(container.querySelector("script")!.innerHTML)();
}

afterEach(() => cleanup());

describe("ThemePaletteClassScript", () => {
  beforeEach(() => {
    root.removeAttribute(themeModeAttribute);
    root.className = "font-variable bg-background";
  });

  it("applies the palette class for the mode already set and follows later mode changes", async () => {
    root.setAttribute(themeModeAttribute, "dark");
    runRenderedScript();

    expect(root.classList.contains(brutalismThemePaletteClasses.dark)).toBe(true);

    await setMode("light");

    expect(root.classList.contains(brutalismThemePaletteClasses.light)).toBe(true);
    expect(root.classList.contains(brutalismThemePaletteClasses.dark)).toBe(false);
    expect(root.getAttribute(themeModeAttribute)).toBe("light");
  });

  it("clears the palette class for an unknown or removed mode and keeps unrelated classes", async () => {
    runRenderedScript();
    await setMode("dark");
    await setMode(null);

    expect(Array.from(root.classList)).toEqual(["font-variable", "bg-background"]);
  });
});

describe("themePaletteClassesFor", () => {
  // These names must equal the selectors HeroUI's theme CSS and design-system exports activate under.
  it("uses HeroUI's plain mode classes for the base theme", () => {
    expect(themePaletteClassesFor("base")).toEqual({ light: "light", dark: "dark" });
  });

  it("prefixes the mode with the Pro theme name", () => {
    expect(themePaletteClassesFor("mouve")).toEqual({ light: "mouve-light", dark: "mouve-dark" });
  });
});

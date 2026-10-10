import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";
import { businessContexts, contextColorClassName } from "../color/businessContext";
import { identityColorClassName, identitySlotForId, identitySlots } from "../color/identityColor";

const css = readFileSync(resolve(process.cwd(), "src/hue-roles.css"), "utf8");

interface TintMember {
  lightness: number;
  chroma: number;
}

type TintFamily = Record<"base" | "foreground" | "soft" | "softForeground", TintMember>;

interface HueRole {
  hue: number;
  chroma: number;
}

const lightFamily = readFamily(blockAfter(".vitarish-tint {"));
const darkFamily = readFamily(blockAfter(':is(.dark, [data-theme="dark"]) .vitarish-tint {'));
const contextRoles = readHueRoles("context");
const identityRoles = readHueRoles("identity");

describe("hue roles", () => {
  it("defines exactly the registered business contexts", () => {
    expect([...contextRoles.keys()]).toEqual([...businessContexts]);
  });

  it("defines exactly the registered identity slots", () => {
    expect([...identityRoles.keys()]).toEqual(identitySlots.map(String));
  });

  it("builds class names the stylesheet defines", () => {
    expect(contextColorClassName("visit")).toBe("vitarish-tint vitarish-context--visit");
    expect(identityColorClassName(3)).toBe("vitarish-tint vitarish-identity--3");
  });

  it("gives a record ID the same identity slot every time and spreads IDs across slots", () => {
    const ids = Array.from({ length: 64 }, (_, index) => `record-${index}`);
    expect(ids.map(identitySlotForId)).toEqual(ids.map(identitySlotForId));
    expect(new Set(ids.map(identitySlotForId)).size).toBe(identitySlots.length);
  });

  it("keeps context hues out of the status bands and away from the brand accent", () => {
    const reservedBands: Array<[number, number]> = [
      [0, 30],
      [55, 75],
      [145, 170],
      [172, 192],
    ];

    for (const [context, role] of contextRoles) {
      if (role.chroma === 0) continue;
      for (const [from, to] of reservedBands) {
        expect(
          role.hue >= from && role.hue <= to,
          `${context} hue ${role.hue} falls in reserved band ${from}-${to}`,
        ).toBe(false);
      }
    }
  });

  it.each([
    ["light", lightFamily],
    ["dark", darkFamily],
  ] as const)("holds WCAG AA text contrast for every role in %s mode", (_mode, family) => {
    for (const [name, role] of [...contextRoles, ...identityRoles]) {
      const base = contrast(color(family.foreground, role), color(family.base, role));
      const soft = contrast(color(family.softForeground, role), color(family.soft, role));
      expect(base, `${name} foreground on base`).toBeGreaterThanOrEqual(4.5);
      expect(soft, `${name} soft-foreground on soft`).toBeGreaterThanOrEqual(4.5);
    }
  });
});

function blockAfter(selector: string): string {
  const start = css.indexOf(selector);
  expect(start, `missing ${selector}`).toBeGreaterThanOrEqual(0);
  return css.slice(start, css.indexOf("}", start));
}

function readFamily(block: string): TintFamily {
  const member = (name: string): TintMember => {
    const match = new RegExp(`--${name}: oklch\\(([\\d.]+)% calc\\(([\\d.]+) \\*`).exec(block);
    expect(match, `missing --${name}`).not.toBeNull();
    return { lightness: Number(match![1]) / 100, chroma: Number(match![2]) };
  };

  return {
    base: member("tint"),
    foreground: member("tint-foreground"),
    soft: member("tint-soft"),
    softForeground: member("tint-soft-foreground"),
  };
}

function readHueRoles(axis: "context" | "identity"): Map<string, HueRole> {
  const pattern = new RegExp(
    `\\.vitarish-${axis}--([a-z0-9-]+) \\{\\s*--tint-hue: (\\d+);(?:\\s*--tint-chroma: (\\d+);)?`,
    "g",
  );
  return new Map(
    [...css.matchAll(pattern)].map((match) => [
      match[1],
      { hue: Number(match[2]), chroma: match[3] === undefined ? 1 : Number(match[3]) },
    ]),
  );
}

/** Relative luminance of an OKLCH color after clipping to the sRGB gamut. */
function color(member: TintMember, role: HueRole): number {
  const chroma = member.chroma * role.chroma;
  const radians = (role.hue * Math.PI) / 180;
  const a = chroma * Math.cos(radians);
  const b = chroma * Math.sin(radians);
  const l = (member.lightness + 0.3963377774 * a + 0.2158037573 * b) ** 3;
  const m = (member.lightness - 0.1055613458 * a - 0.0638541728 * b) ** 3;
  const s = (member.lightness - 0.0894841775 * a - 1.291485548 * b) ** 3;
  const clip = (channel: number) => Math.min(1, Math.max(0, channel));
  const red = clip(4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s);
  const green = clip(-1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s);
  const blue = clip(-0.0041960863 * l - 0.7034186147 * m + 1.707614701 * s);
  return 0.2126 * red + 0.7152 * green + 0.0722 * blue;
}

function contrast(first: number, second: number): number {
  return (Math.max(first, second) + 0.05) / (Math.min(first, second) + 0.05);
}

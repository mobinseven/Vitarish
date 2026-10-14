import { readFileSync } from "node:fs";
import { resolve } from "node:path";
import { describe, expect, it } from "vitest";
import { chromaColors } from "@vitarish/ui";

/** WCAG AA minimum for normal-size text. */
const minimumContrast = 4.5;

const globalsCss = readFileSync(resolve(process.cwd(), "src/app/globals.css"), "utf8");

type Rgb = [number, number, number];

function tokenValue(token: string): string {
  const match = new RegExp(`${token}:\s*([^;]+);`).exec(globalsCss);
  if (!match) throw new Error(`${token} is not defined in globals.css`);
  return match[1].trim();
}

function parseColor(value: string): Rgb {
  const hex = /^#([0-9a-f]{6})$/i.exec(value);
  if (hex) {
    return [0, 2, 4].map((offset) => parseInt(hex[1].slice(offset, offset + 2), 16) / 255) as Rgb;
  }
  const oklch = /^oklch\(\s*([\d.]+)%\s+([\d.]+)\s+([\d.]+)\s*\)$/i.exec(value);
  if (oklch) return oklchToSrgb(Number(oklch[1]) / 100, Number(oklch[2]), Number(oklch[3]));
  throw new Error(`Unsupported color value: ${value}`);
}

function oklchToSrgb(lightness: number, chroma: number, hue: number): Rgb {
  const a = chroma * Math.cos((hue * Math.PI) / 180);
  const b = chroma * Math.sin((hue * Math.PI) / 180);
  const l = (lightness + 0.3963377774 * a + 0.2158037573 * b) ** 3;
  const m = (lightness - 0.1055613458 * a - 0.0638541728 * b) ** 3;
  const s = (lightness - 0.0894841775 * a - 1.291485548 * b) ** 3;
  const linear = [
    4.0767416621 * l - 3.3077115913 * m + 0.2309699292 * s,
    -1.2684380046 * l + 2.6097574011 * m - 0.3413193965 * s,
    -0.0041960863 * l - 0.7034186147 * m + 1.707614701 * s,
  ];
  return linear.map((channel) => {
    const encoded = channel <= 0.0031308 ? 12.92 * channel : 1.055 * channel ** (1 / 2.4) - 0.055;
    return Math.min(1, Math.max(0, encoded));
  }) as Rgb;
}

function relativeLuminance(color: Rgb): number {
  const [r, g, b] = color.map((channel) =>
    channel <= 0.04045 ? channel / 12.92 : ((channel + 0.055) / 1.055) ** 2.4,
  );
  return 0.2126 * r + 0.7152 * g + 0.0722 * b;
}

function contrastRatio(first: Rgb, second: Rgb): number {
  const [lighter, darker] = [relativeLuminance(first), relativeLuminance(second)].sort((x, y) => y - x);
  return (lighter + 0.05) / (darker + 0.05);
}

describe("owner chroma colors", () => {
  it.each(chromaColors)("keep chroma-%s text readable at WCAG AA", (color) => {
    const background = parseColor(tokenValue(`--chroma-${color}`));
    const foreground = parseColor(tokenValue(`--chroma-${color}-foreground`));

    expect(contrastRatio(background, foreground)).toBeGreaterThanOrEqual(minimumContrast);
  });
});

describe("owner anchor color", () => {
  it("keeps hero band text readable at WCAG AA", () => {
    const background = parseColor(tokenValue("--anchor"));
    const foreground = parseColor(tokenValue("--anchor-foreground"));

    expect(contrastRatio(background, foreground)).toBeGreaterThanOrEqual(minimumContrast);
  });
});

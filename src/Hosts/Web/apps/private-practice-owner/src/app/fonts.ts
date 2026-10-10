import { Inter, Parastoo } from "next/font/google";

export const inter = Inter({
  display: "swap",
  subsets: ["latin"],
  variable: "--font-inter",
});

/*
 * Next's font catalog lists no `arabic` subset or fallback metrics for
 * Parastoo, so no subset can be preloaded for RTL text and no metric-adjusted
 * fallback exists; the Arabic-script faces are still self-hosted and selected
 * by their unicode ranges.
 */
export const parastoo = Parastoo({
  adjustFontFallback: false,
  display: "swap",
  preload: false,
  variable: "--font-parastoo",
});

export const applicationFontVariables = [inter.variable, parastoo.variable].join(" ");

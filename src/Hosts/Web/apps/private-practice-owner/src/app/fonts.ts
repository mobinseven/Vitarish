import { Inter, Vazirmatn } from "next/font/google";

export const inter = Inter({
  display: "swap",
  subsets: ["latin"],
  variable: "--font-inter",
});

export const vazirmatn = Vazirmatn({
  display: "swap",
  subsets: ["arabic"],
  variable: "--font-vazirmatn",
});

export const applicationFontVariables = [inter.variable, vazirmatn.variable].join(" ");

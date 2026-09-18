import { Hanken_Grotesk, IBM_Plex_Mono, Vazirmatn } from "next/font/google";

export const hankenGrotesk = Hanken_Grotesk({
  display: "swap",
  subsets: ["latin"],
  variable: "--font-hanken-grotesk",
});

export const ibmPlexMono = IBM_Plex_Mono({
  display: "swap",
  subsets: ["latin"],
  variable: "--font-ibm-plex-mono",
  weight: ["400", "500", "600", "700"],
});

export const vazirmatn = Vazirmatn({
  display: "swap",
  subsets: ["arabic"],
  variable: "--font-vazirmatn",
});

export const applicationFontVariables = [
  hankenGrotesk.variable,
  ibmPlexMono.variable,
  vazirmatn.variable,
].join(" ");

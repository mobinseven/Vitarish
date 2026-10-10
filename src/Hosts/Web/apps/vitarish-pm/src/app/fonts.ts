import { Cairo, IBM_Plex_Mono, Michroma } from "next/font/google";

export const michroma = Michroma({
  display: "swap",
  subsets: ["latin"],
  variable: "--font-michroma",
  weight: "400",
});

export const ibmPlexMono = IBM_Plex_Mono({
  display: "swap",
  subsets: ["latin"],
  variable: "--font-ibm-plex-mono",
  weight: ["400", "500", "600", "700"],
});

export const cairo = Cairo({
  display: "swap",
  subsets: ["arabic"],
  variable: "--font-cairo",
});

export const applicationFontVariables = [
  michroma.variable,
  ibmPlexMono.variable,
  cairo.variable,
].join(" ");

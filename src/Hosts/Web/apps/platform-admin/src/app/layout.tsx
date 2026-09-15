import type { Viewport } from "next";
import { headers } from "next/headers";
import { I18nAppLayout } from "@vitarish/localization";
import { Providers } from "@/components/Providers";
import { getMessages } from "@/localization/messages";
import "./globals.css";

export const metadata = { title: "Vitarish Platform Administration", description: "Vitarish platform operator control plane" };
export const viewport: Viewport = { viewportFit: "cover" };

export default async function RootLayout({ children }: { children: React.ReactNode }) {
  const headerStore = await headers();
  return <><head><link rel="stylesheet" href="/fontawesome/css/all.min.css" /></head><I18nAppLayout cookieHeader={headerStore.get("cookie")} acceptLanguageHeader={headerStore.get("accept-language")} getMessages={getMessages} htmlClassName="bg-background text-foreground" bodyClassName="bg-background text-foreground font-sans antialiased"><Providers>{children}</Providers></I18nAppLayout></>;
}

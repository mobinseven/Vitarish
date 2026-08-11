import type { Viewport } from "next";
import { headers } from "next/headers";
import { I18nAppLayout } from "@vitarish/web-localization";
import { Providers } from "@/components/Providers";
import { getMessages } from "@/localization/messages";
import { Vazirmatn } from "next/font/google";
import "./globals.css";

const vazirmatn = Vazirmatn({
  subsets: ["arabic"],
  variable: "--font-vazirmatn",
  display: "swap",
});

export const metadata = {
  title: "Vitarish PM",
  description: "SEA project management",
};
export const viewport: Viewport = { viewportFit: "cover" };

export default async function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const headerStore = await headers();
  return (
    <>
      <head>
        <link rel="stylesheet" href="/fontawesome/css/all.min.css" />
        <link
          href="https://fonts.googleapis.com/css2?family=Vazirmatn:wght@100..900&display=swap"
          rel="stylesheet"
        />
        <link
          href="https://fonts.googleapis.com/css2?family=Hanken+Grotesk:wght@100..900&display=swap"
          rel="stylesheet"
        />
        <link
          href="https://fonts.googleapis.com/css2?family=IBM+Plex+Mono:wght@400;500;600;700&display=swap"
          rel="stylesheet"
        />
      </head>
      <I18nAppLayout
        cookieHeader={headerStore.get("cookie")}
        acceptLanguageHeader={headerStore.get("accept-language")}
        getMessages={getMessages}
        htmlClassName="bg-background text-foreground"
        bodyClassName="bg-background text-foreground font-sans antialiased"
      >
        <Providers>{children}</Providers>
      </I18nAppLayout>
    </>
  );
}

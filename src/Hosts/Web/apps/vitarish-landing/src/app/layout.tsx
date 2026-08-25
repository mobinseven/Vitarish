import type { Metadata } from "next";
import { headers } from "next/headers";
import { Vazirmatn } from "next/font/google";
import { I18nAppLayout, resolveI18nAppLocale } from "@vitarish/localization";
import { AppProviders } from "../components/providers/AppProviders";
import { getMessages } from "../localization";
import "./globals.css";

const vazirmatn = Vazirmatn({
  subsets: ["arabic"],
  variable: "--font-vazirmatn",
  display: "swap",
});

export async function generateMetadata(): Promise<Metadata> {
  const headerStore = await headers();
  const { locale } = resolveI18nAppLocale({
    cookieHeader: headerStore.get("cookie"),
    acceptLanguageHeader: headerStore.get("accept-language"),
  });
  const messages = getMessages(locale) as { metadata: { root: { title: string; description: string } } };
  return {
    title: messages.metadata.root.title,
    description: messages.metadata.root.description,
    icons: { icon: "/512.webp", apple: "/512.webp" },
  };
}
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
        <link href="https://fonts.googleapis.com/css2?family=Vazirmatn:wght@100..900&display=swap" rel="stylesheet" />
        <link href="https://fonts.googleapis.com/css2?family=Hanken+Grotesk:wght@100..900&display=swap" rel="stylesheet" />
        <link href="https://fonts.googleapis.com/css2?family=IBM+Plex+Mono:wght@400;500;600;700&display=swap" rel="stylesheet" />
      </head>
      <I18nAppLayout
        cookieHeader={headerStore.get("cookie")}
        acceptLanguageHeader={headerStore.get("accept-language")}
        getMessages={getMessages}
        htmlClassName={vazirmatn.variable}
        bodyClassName="bg-background text-foreground"
      >
        <AppProviders>{children}</AppProviders>
      </I18nAppLayout>
    </>
  );
}

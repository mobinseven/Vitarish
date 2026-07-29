import type { Metadata } from "next";
import { headers } from "next/headers";
import { Vazirmatn } from "next/font/google";
import { I18nAppLayout } from "@vitarish/web-localization";
import { getMessages } from "../localization";
import "./globals.css";

const vazirmatn = Vazirmatn({
  subsets: ["arabic"],
  variable: "--font-vazirmatn",
  display: "swap",
});

export const metadata: Metadata = {
  title: "Vitarish — The glory of purpose",
  description: "Purpose-oriented AI services for people and small businesses.",
  icons: { icon: "/512.webp", apple: "/512.webp" },
};
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
      </head>
      <I18nAppLayout
        cookieHeader={headerStore.get("cookie")}
        acceptLanguageHeader={headerStore.get("accept-language")}
        getMessages={getMessages}
        htmlClassName={vazirmatn.variable}
        bodyClassName="bg-background text-foreground"
      >
        {children}
      </I18nAppLayout>
    </>
  );
}

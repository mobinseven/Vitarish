import type { Metadata } from "next";
import { headers } from "next/headers";
import { I18nAppLayout, resolveI18nAppLocale } from "@vitarish/localization";
import { ContentSecurityPolicyNonceMeta, contentSecurityPolicyNonce } from "@vitarish/ui/content-security-policy";
import { AppProviders } from "../components/providers/AppProviders";
import { getMessages } from "../localization";
import { applicationFontVariables } from "./fonts";
import "./globals.css";

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
  const nonce = contentSecurityPolicyNonce(headerStore);

  return (
    <>
      <head>
        <link rel="stylesheet" href="/fontawesome/css/all.min.css" />
        <ContentSecurityPolicyNonceMeta nonce={nonce} />
      </head>
      <I18nAppLayout
        cookieHeader={headerStore.get("cookie")}
        acceptLanguageHeader={headerStore.get("accept-language")}
        getMessages={getMessages}
        htmlClassName={applicationFontVariables}
        bodyClassName="bg-background text-foreground"
      >
        <AppProviders nonce={nonce}>{children}</AppProviders>
      </I18nAppLayout>
    </>
  );
}

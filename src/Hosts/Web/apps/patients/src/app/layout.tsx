import { NavigationTransitionDocument, navigationTransitionDocument } from "@vitarish/ui/navigation-document";
import { Routes } from "./routes";
import { CentralAuthenticationSessionRenewal } from "@vitarish/auth/central-server";
import type { Viewport } from "next";
import { headers } from "next/headers";
import { Providers } from "@/components/providers/AppProviders";
import { ContentSecurityPolicyNonceMeta, contentSecurityPolicyNonce } from "@vitarish/ui/content-security-policy";
import { getMessages, I18nAppLayout, localizedMetadata } from "@/localization";
import "./globals.css";

export const metadata = localizedMetadata(
  "metadata.root.title",
  "metadata.root.description",
);

export const viewport: Viewport = { viewportFit: "cover" };

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
        htmlClassName="bg-background text-foreground"
        bodyClassName="bg-background text-foreground font-sans antialiased"
      >
        <NavigationTransitionDocument nonce={nonce} signInHref={Routes.Login.Index} homeHref={Routes.Root} />
        <div {...{ [navigationTransitionDocument.contentAttribute]: "" }} inert suppressHydrationWarning>
          <Providers nonce={nonce}><CentralAuthenticationSessionRenewal />{children}</Providers>
        </div>
      </I18nAppLayout>
    </>
  );
}

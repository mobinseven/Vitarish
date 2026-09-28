import type { Metadata, Viewport } from "next";
import { headers } from "next/headers";
import { Providers } from "@/components/providers/AppProviders";
import { contentSecurityPolicyNonce } from "@vitarish/ui/content-security-policy";
import { ContentSecurityPolicyNonceMeta } from "@vitarish/ui/content-security-policy-nonce-meta";
import { getMessages, I18nAppLayout } from "@/localization";
import "./globals.css";

export const metadata: Metadata = {
  title: "Knowledge QA Demo | Vitarish",
  description: "Knowledge QA demo application.",
};

export const viewport: Viewport = { viewportFit: "cover" };

export default async function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const headerStore = await headers();
  const nonce = contentSecurityPolicyNonce(headerStore);

  return (
    <I18nAppLayout
      documentHead={<><ContentSecurityPolicyNonceMeta nonce={nonce} /><link rel="stylesheet" href="/fontawesome/css/all.min.css" /></>}
        cookieHeader={headerStore.get("cookie")}
        acceptLanguageHeader={headerStore.get("accept-language")}
        getMessages={getMessages}
        bodyClassName="bg-background font-sans antialiased"
      >
        <Providers nonce={nonce}>{children}</Providers>
      </I18nAppLayout>
  );
}

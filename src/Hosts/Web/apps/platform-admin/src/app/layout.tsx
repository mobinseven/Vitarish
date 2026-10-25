import type { Viewport } from "next";
import { headers } from "next/headers";
import { I18nAppLayout } from "@vitarish/localization";
import { Providers } from "@/components/Providers";
import { PlatformAdminAccessBoundary } from "@/components/PlatformAdminAccessBoundary";
import { getMessages } from "@/localization/messages";
import { ContentSecurityPolicyNonceMeta, contentSecurityPolicyNonce } from "@vitarish/ui/content-security-policy";
import "./globals.css";

export const metadata = { title: "Vitarish Platform Administration", description: "Vitarish platform operator control plane" };
export const viewport: Viewport = { viewportFit: "cover" };

export default async function RootLayout({ children }: { children: React.ReactNode }) {
  const headerStore = await headers();
  const nonce = contentSecurityPolicyNonce(headerStore);
  return <>
    <head>
      <link rel="stylesheet" href="/fontawesome/css/all.min.css" />
      <ContentSecurityPolicyNonceMeta nonce={nonce} /></head>
    <I18nAppLayout cookieHeader={headerStore.get("cookie")} acceptLanguageHeader={headerStore.get("accept-language")} getMessages={getMessages} htmlClassName="bg-background text-foreground" bodyClassName="bg-background text-foreground font-sans antialiased">
      <header className="w-full">
        <picture>
          <source media="(prefers-color-scheme: dark)" srcSet="/Master-Hel-Logotype-Dark.svg" />
          <img src="/Master-Hel-Logotype.svg" alt="Vitarish" className="block h-auto w-full" />
        </picture>
      </header>
      <Providers nonce={nonce}><PlatformAdminAccessBoundary>{children}</PlatformAdminAccessBoundary></Providers>
      <header className="w-full">
        <picture>
          <source media="(prefers-color-scheme: dark)" srcSet="/Master-Hel-Logotype-Dark-©.svg" />
          <img src="/Master-Hel-Logotype-©.svg" alt="Vitarish" className="block h-auto w-full" />
        </picture>
      </header>
    </I18nAppLayout>
  </>;
}

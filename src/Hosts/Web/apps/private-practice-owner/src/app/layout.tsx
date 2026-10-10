import type { Viewport } from "next";
import { headers } from "next/headers";
import { Providers } from "@/components/providers/AppProviders";
import { ContentSecurityPolicyNonceMeta, contentSecurityPolicyNonce } from "@vitarish/ui/content-security-policy";
import { ThemePaletteClassScript } from "@vitarish/ui/theme-palette";
import { InitialPageLoader } from "@/components/loading/InitialPageLoader";
import { getMessages, I18nAppLayout, localizedMetadata } from "@/localization";
import { applicationFontVariables } from "./fonts";
import { themePaletteClasses } from "./theme";
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
        <ThemePaletteClassScript classes={themePaletteClasses} nonce={nonce} />
      </head>
      <I18nAppLayout
        cookieHeader={headerStore.get("cookie")}
        acceptLanguageHeader={headerStore.get("accept-language")}
        getMessages={getMessages}
        htmlClassName={`${applicationFontVariables} bg-background text-foreground`}
        vibrantPalette
        bodyClassName="bg-background text-foreground font-sans antialiased"
      >
        <Providers nonce={nonce}>
          <InitialPageLoader />
          {children}
        </Providers>
      </I18nAppLayout>
    </>
  );
}

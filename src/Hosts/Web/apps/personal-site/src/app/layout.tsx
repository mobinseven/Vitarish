import type { Metadata } from "next";
import type { ReactNode } from "react";
import { PublicDocumentShell, StandaloneDocumentPage } from "@vitarish/ui/layout";
import { ThemePaletteClassScript } from "@vitarish/ui/theme-palette";
import { SiteNavigation } from "../components/SiteNavigation";
import { SiteThemeProvider } from "../components/SiteThemeProvider";
import { siteContent } from "../content";
import { themePaletteClasses } from "./theme";
import "./globals.css";

export const metadata: Metadata = {
  title: siteContent.name,
  description: siteContent.intro,
};

export default function RootLayout({ children }: { children: ReactNode }) {
  return (
    <html lang="en" suppressHydrationWarning>
      <head>
        <ThemePaletteClassScript classes={themePaletteClasses} />
      </head>
      <body className="bg-background text-foreground">
        <SiteThemeProvider>
          <PublicDocumentShell
            header={<SiteNavigation />}
            footer={
              <p className="p-6 text-center text-sm text-muted">
                © {new Date().getFullYear()} {siteContent.name}
              </p>
            }
          >
            <StandaloneDocumentPage className="grid place-items-center px-4 py-8">
              {children}
            </StandaloneDocumentPage>
          </PublicDocumentShell>
        </SiteThemeProvider>
      </body>
    </html>
  );
}

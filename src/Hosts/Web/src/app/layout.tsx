import type { Metadata } from "next";
import { Providers } from "@/components/providers/HeroUIProvider";
import { AppBar } from "@/components/app-bar/AppBar";
import { getCultureDirection } from "@/i18n/cultures";
import { createLocalizer } from "@/i18n/localizer";
import { resolveRequestCulture } from "@/i18n/server";
import "./globals.css";

export async function generateMetadata(): Promise<Metadata> {
  const culture = await resolveRequestCulture();
  const { t } = createLocalizer(culture);

  return {
    title: t("App.BrandName"),
    description: t("App.Description"),
  };
}

export default async function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const culture = await resolveRequestCulture();

  return (
    <html lang={culture} dir={getCultureDirection(culture)} suppressHydrationWarning>
      <body>
        <Providers culture={culture}>
          <AppBar />
          {children}
        </Providers>
      </body>
    </html>
  );
}

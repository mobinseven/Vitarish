import { headers } from "next/headers";
import { Providers } from "@/components/providers/AppProviders";
import { getLocaleDirection, localizedMetadata, parseLocaleCookie, resolveLocale } from "@/localization";
import "./globals.css";

export const metadata = localizedMetadata("metadata.root.title", "metadata.root.description");

async function resolveRequestLocale() {
  const headerStore = await headers();
  const acceptLanguageLocales = headerStore
    .get("accept-language")
    ?.split(",")
    .map((candidate) => candidate.split(";")[0]?.trim())
    .filter(Boolean);

  return resolveLocale({
    cookieLocale: parseLocaleCookie(headerStore.get("cookie")),
    acceptLanguageLocales,
  }).locale;
}

export default async function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const locale = await resolveRequestLocale();
  const direction = getLocaleDirection(locale);

  return (
    <html lang={locale} dir={direction} suppressHydrationWarning className="bg-background text-foreground">
      <body className="bg-background text-foreground font-sans antialiased">
        <Providers locale={locale}>
          {children}
        </Providers>
      </body>
    </html>
  );
}

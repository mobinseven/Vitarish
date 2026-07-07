import { headers } from "next/headers";
import { Inter, JetBrains_Mono, Space_Grotesk } from "next/font/google";
import "../lib/fa/v7/css/all.min.css";
import { Providers } from "@/components/providers/AppProviders";
import { getLocaleDirection, localizedMetadata, parseLocaleCookie, resolveLocale } from "@/localization";
import "./globals.css";

const inter = Inter({ subsets: ["latin"], variable: "--font-sans" });
const spaceGrotesk = Space_Grotesk({ subsets: ["latin"], variable: "--font-display" });
const jetbrainsMono = JetBrains_Mono({ subsets: ["latin"], variable: "--font-mono" });

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
    <html lang={locale} dir={direction} suppressHydrationWarning className={`${inter.variable} ${spaceGrotesk.variable} ${jetbrainsMono.variable} bg-background text-foreground`}>
      <body className="bg-background text-foreground font-sans antialiased">
        <Providers locale={locale}>
          {children}
        </Providers>
      </body>
    </html>
  );
}

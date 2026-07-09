import type { Metadata } from "next";
import { Providers } from "@/components/providers/AppProviders";
import { defaultLocale } from "@/localization/locales";
import "./globals.css";

export const metadata: Metadata = {
  title: "Knowledge QA Demo | Vitarish",
  description: "Knowledge QA demo application.",
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en" suppressHydrationWarning>
      <body className="min-h-screen bg-background font-sans antialiased">
        <Providers locale={defaultLocale}>{children}</Providers>
      </body>
    </html>
  );
}

import type { Metadata } from "next";
import { Providers } from "@/components/providers/HeroUIProvider";
import "./globals.css";

export const metadata: Metadata = {
  title: "Vitarish",
  description: "Vitarish platform authentication",
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="fa" suppressHydrationWarning>
      <body>
        <Providers>{children}</Providers>
      </body>
    </html>
  );
}

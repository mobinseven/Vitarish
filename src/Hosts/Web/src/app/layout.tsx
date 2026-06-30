import type { Metadata } from "next";
import { Providers } from "@/components/providers/HeroUIProvider";
import { AppBar } from "@/components/app-bar/AppBar";
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
    <html lang="en" suppressHydrationWarning>
      <body>
        <Providers>
          <AppBar />
          {children}
        </Providers>
      </body>
    </html>
  );
}

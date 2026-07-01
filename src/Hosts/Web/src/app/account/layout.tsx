"use client";

import { useEffect } from "react";
import { useRouter, usePathname } from "next/navigation";
import Link from "next/link";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import { JourneyGateBoundary } from "@/components/journey/JourneyGateBoundary";
import { Routes } from "@/app/routes";
import { useLocalizedText, type MessageKey } from "@/localization";

const navLinks = [
  { href: Routes.Account.Index, labelKey: "account.navigation.settings" as MessageKey, icon: "fa-solid fa-gear" },
  { href: Routes.Account.Name, labelKey: "account.navigation.name" as MessageKey, icon: "fa-solid fa-person" },
  { href: Routes.Account.Email, labelKey: "account.navigation.email" as MessageKey, icon: "fa-solid fa-envelope" },
  { href: Routes.Account.Phone, labelKey: "account.navigation.phone" as MessageKey, icon: "fa-solid fa-mobile" },
  { href: Routes.Account.Password, labelKey: "account.navigation.password" as MessageKey, icon: "fa-solid fa-lock" },
  { href: Routes.Account.Passkeys, labelKey: "account.navigation.passkeys" as MessageKey, icon: "fa-solid fa-key" },
  { href: Routes.Account.Theme, labelKey: "account.navigation.theme" as MessageKey, icon: "fa-solid fa-palette" },
  { href: Routes.Account.Culture, labelKey: "account.navigation.language" as MessageKey, icon: "fa-solid fa-globe" },
];

export default function AccountLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const { isAuthenticated, isLoading } = useAuthSession();
  const router = useRouter();
  const pathname = usePathname();
  const { t } = useLocalizedText();

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      router.push(Routes.Login.Index);
    }
  }, [isLoading, isAuthenticated, router]);

  if (isLoading) {
    return (
      <main className="grid min-h-screen place-items-center">
        <p className="text-default-500">{t("common.states.loading")}</p>
      </main>
    );
  }

  if (!isAuthenticated) {
    return null;
  }

  return (
    <div className="flex min-h-screen">
      <aside className="w-64 border-e border-default-200 p-4">
        <h2 className="text-lg font-bold mb-4">{t("account.title")}</h2>
        <nav className="flex flex-col gap-1">
          {navLinks.map((link) => {
            const isActive =
              link.href === Routes.Account.Index
                ? pathname === Routes.Account.Index
                : pathname === link.href || pathname.startsWith(`${link.href}/`);
            return (
              <Link
                key={link.href}
                href={link.href}
                className={`flex items-center gap-3 rounded-lg px-3 py-2 text-sm transition-colors ${
                  isActive
                    ? "bg-accent-soft text-accent font-medium"
                    : "text-default-500 hover:text-foreground hover:bg-default-100"
                }`}
              >
                <i className={`${link.icon} w-4 text-center text-xs`} />
                {t(link.labelKey)}
              </Link>
            );
          })}
        </nav>
      </aside>
      <main className="flex-1 p-8">
        <JourneyGateBoundary>{children}</JourneyGateBoundary>
      </main>
    </div>
  );
}

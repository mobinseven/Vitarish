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
      <main className="ds-app-shell ds-centered-shell">
        <p className="text-muted">{t("common.states.loading")}</p>
      </main>
    );
  }

  if (!isAuthenticated) {
    return null;
  }

  return (
    <div className="ds-account-layout">
      <aside className="sidebar">
        <h2 className="text-lg font-bold mb-4 px-3">{t("account.title")}</h2>
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
                className={`sidebar-nav-item ${isActive ? "sidebar-nav-item-active" : ""}`}
              >
                <i className={`${link.icon} w-4 text-center text-xs`} />
                {t(link.labelKey)}
              </Link>
            );
          })}
        </nav>
      </aside>
      <main className="ds-account-content">
        <JourneyGateBoundary>{children}</JourneyGateBoundary>
      </main>
    </div>
  );
}

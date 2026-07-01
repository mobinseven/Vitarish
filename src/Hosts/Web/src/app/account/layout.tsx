"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import { JourneyGateBoundary } from "@/components/journey/JourneyGateBoundary";
import { AppShellWrapper, Sidebar } from "@/components/app-shell";
import { Routes } from "@/app/routes";
import { useLocalizedText, type MessageKey } from "@/localization";

const sidebarLinkDefs = [
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

  const resolvedLinks = sidebarLinkDefs.map((link) => ({
    href: link.href,
    label: t(link.labelKey),
    icon: link.icon,
  }));

  return (
    <AppShellWrapper
      sidebar={
        <Sidebar
          title={t("account.title")}
          links={resolvedLinks}
          exactMatchHrefs={[Routes.Account.Index]}
        />
      }
    >
      <div className="p-8">
        <JourneyGateBoundary>{children}</JourneyGateBoundary>
      </div>
    </AppShellWrapper>
  );
}

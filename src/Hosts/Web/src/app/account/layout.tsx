"use client";

import { useEffect } from "react";
import { useRouter, usePathname } from "next/navigation";
import Link from "next/link";
import { AppLayout, Navbar, Sidebar } from "@heroui-pro/react";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import { JourneyGateBoundary } from "@/components/journey/JourneyGateBoundary";
import { ThemeSwitch } from "@/components/shell/ThemeSwitch";
import { UserMenu } from "@/components/shell/UserMenu";
import { Routes } from "@/app/routes";
import { useLocalizedText, type MessageKey } from "@/localization";

const sidebarLinkDefs = [
  { href: Routes.Account.Index, labelKey: "account.navigation.settings" as MessageKey, icon: "fa-solid fa-gear", exactMatch: true },
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
        <p className="text-muted">{t("common.states.loading")}</p>
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
    exactMatch: link.exactMatch,
  }));

  return (
    <AppLayout
      navigate={(href) => router.push(href)}
      sidebarCollapsible="icon"
      sidebar={
        <Sidebar.Root>
          <Sidebar.Header>
            <Sidebar.MenuLabel>{t("account.title")}</Sidebar.MenuLabel>
          </Sidebar.Header>
          <Sidebar.Content>
            <Sidebar.Group>
              <Sidebar.Menu>
                {resolvedLinks.map((link) => {
                  const isExact = link.exactMatch ?? false;
                  const isActive = isExact
                    ? pathname === link.href
                    : pathname === link.href || pathname.startsWith(`${link.href}/`);

                  return (
                    <Sidebar.MenuItem
                      key={link.href}
                      href={link.href}
                      isCurrent={isActive}
                    >
                      <Sidebar.MenuIcon>
                        <i className={link.icon} />
                      </Sidebar.MenuIcon>
                      <Sidebar.MenuLabel>{link.label}</Sidebar.MenuLabel>
                    </Sidebar.MenuItem>
                  );
                })}
              </Sidebar.Menu>
            </Sidebar.Group>
          </Sidebar.Content>
        </Sidebar.Root>
      }
      navbar={
        <Navbar.Root>
          <Navbar.Header>
            <Navbar.Brand>
              <Link href="/" className="no-underline text-inherit">
                {t("appShell.brand")}
              </Link>
            </Navbar.Brand>
            <Navbar.Spacer />
            <Navbar.Content className="hidden sm:flex items-center gap-2">
              <ThemeSwitch />
              <UserMenu />
            </Navbar.Content>
            <Navbar.MenuToggle srLabel={t("appShell.menu.open")} />
          </Navbar.Header>
        </Navbar.Root>
      }
    >
      <div className="p-8">
        <JourneyGateBoundary>{children}</JourneyGateBoundary>
      </div>
    </AppLayout>
  );
}

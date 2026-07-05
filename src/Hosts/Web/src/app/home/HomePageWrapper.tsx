"use client";

import { useRouter } from "next/navigation";
import Link from "next/link";
import { AppLayout, Navbar } from "@heroui-pro/react";
import { ThemeSwitch } from "@/components/shell/ThemeSwitch";
import { UserMenu } from "@/components/shell/UserMenu";
import { Routes } from "@/app/routes";
import { useLocalizedText } from "@/localization";

export function HomePageWrapper({ children }: { children: React.ReactNode }) {
  const router = useRouter();
  const { t } = useLocalizedText();

  return (
    <AppLayout
      navigate={(href) => router.push(href)}
      navbar={
        <Navbar.Root>
          <Navbar.Header>
            <Navbar.Brand>
              <Link href="/" className="no-underline text-inherit">
                {t("appShell.brand")}
              </Link>
            </Navbar.Brand>
            <Navbar.Content className="hidden lg:flex">
              <Navbar.Item href={Routes.Home} isCurrent>
                {t("appShell.navigation.home")}
              </Navbar.Item>
              <Navbar.Item href={Routes.Knowledge.Index}>
                {t("appShell.navigation.knowledge")}
              </Navbar.Item>
            </Navbar.Content>
            <Navbar.Spacer />
            <Navbar.Content className="hidden sm:flex items-center gap-2">
              <ThemeSwitch />
              <UserMenu />
            </Navbar.Content>
            <Navbar.MenuToggle srLabel={t("appShell.menu.open")} />
          </Navbar.Header>
          <Navbar.Menu>
            <Navbar.MenuItem href={Routes.Home} isCurrent>
              {t("appShell.navigation.home")}
            </Navbar.MenuItem>
            <Navbar.MenuItem href={Routes.Knowledge.Index}>
              {t("appShell.navigation.knowledge")}
            </Navbar.MenuItem>
            <div className="flex items-center gap-2 mt-4">
              <ThemeSwitch />
              <UserMenu />
            </div>
          </Navbar.Menu>
        </Navbar.Root>
      }
    >
      {children}
    </AppLayout>
  );
}

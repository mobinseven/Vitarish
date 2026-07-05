"use client";

import { useRouter, usePathname } from "next/navigation";
import Link from "next/link";
import { AppLayout, Navbar, Sidebar } from "@heroui-pro/react";
import { AuthorizationGuard } from "@/components/auth/AuthorizationGuard";
import { JourneyGateBoundary } from "@/components/journey/JourneyGateBoundary";
import { ThemeSwitch } from "@/components/shell/ThemeSwitch";
import { UserMenu } from "@/components/shell/UserMenu";
import { Routes } from "@/app/routes";
import { useLocalizedText } from "@/localization";

export default function ClinicUserLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const router = useRouter();
  const pathname = usePathname();
  const { t } = useLocalizedText();

  const sidebarLinks = [
    { href: Routes.ClinicUser.Shifts.Index, label: t("clinic.userAppointments.shifts.title"), icon: "fa-solid fa-clock" },
    { href: Routes.ClinicUser.Reservations.Index, label: t("clinic.userAppointments.reservations.title"), icon: "fa-solid fa-calendar-check" },
  ];

  return (
    <AuthorizationGuard requiredRole="user">
      <AppLayout
        navigate={(href) => router.push(href)}
        sidebarCollapsible="icon"
        sidebar={
          <Sidebar.Root>
            <Sidebar.Header>
              <Sidebar.MenuLabel>{t("appShell.navigation.clinic")}</Sidebar.MenuLabel>
            </Sidebar.Header>
            <Sidebar.Content>
              <Sidebar.Group>
                <Sidebar.Menu>
                  {sidebarLinks.map((link) => {
                    const isActive = pathname === link.href || pathname.startsWith(`${link.href}/`);
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
    </AuthorizationGuard>
  );
}

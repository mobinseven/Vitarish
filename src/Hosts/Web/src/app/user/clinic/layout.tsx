"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { AuthorizationGuard } from "@/components/auth/AuthorizationGuard";
import { JourneyGateBoundary } from "@/components/journey/JourneyGateBoundary";
import { Routes } from "@/app/routes";
import { useLocalizedText } from "@/localization";

export default function ClinicUserLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const pathname = usePathname();
  const { t } = useLocalizedText();

  const navLinks = [
    { href: Routes.ClinicUser.Shifts.Index, label: t("clinic.userAppointments.shifts.title"), icon: "fa-solid fa-clock" },
    { href: Routes.ClinicUser.Reservations.Index, label: t("clinic.userAppointments.reservations.title"), icon: "fa-solid fa-calendar-check" },
  ];

  return (
    <AuthorizationGuard requiredRole="user">
      <div className="flex min-h-screen">
        <aside className="w-64 border-e border-default-200 p-4">
          <h2 className="text-lg font-bold mb-4">{t("appShell.navigation.clinic")}</h2>
          <nav className="flex flex-col gap-1">
            {navLinks.map((link) => {
              const isActive =
                pathname === link.href || pathname.startsWith(`${link.href}/`);
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
                  {link.label}
                </Link>
              );
            })}
          </nav>
        </aside>
        <main className="flex-1 p-8">
          <JourneyGateBoundary>{children}</JourneyGateBoundary>
        </main>
      </div>
    </AuthorizationGuard>
  );
}

"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { AuthorizationGuard } from "@/components/auth/AuthorizationGuard";
import { JourneyGateBoundary } from "@/components/journey/JourneyGateBoundary";
import { Routes } from "@/app/routes";
import { useLocalizedText } from "@/localization";

export default function ClinicLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const pathname = usePathname();
  const { t } = useLocalizedText();

  const navLinks = [
    { href: Routes.Clinic.Index, label: t("clinic.dashboard.title"), icon: "fa-solid fa-gauge" },
    { href: Routes.Clinic.Patients.Index, label: t("clinic.patients.title"), icon: "fa-solid fa-users" },
    { href: Routes.Clinic.CheckIn, label: t("clinic.dashboard.sections.checkIn.title"), icon: "fa-solid fa-clipboard-check" },
    { href: Routes.Clinic.Appointments.Reservations.Index, label: t("clinic.dashboard.sections.appointments.title"), icon: "fa-solid fa-calendar" },
    { href: Routes.Clinic.Reminders, label: t("clinic.dashboard.sections.reminders.title"), icon: "fa-solid fa-bell" },
    { href: Routes.Clinic.Settings.Index, label: t("clinic.dashboard.sections.settings.title"), icon: "fa-solid fa-gear" },
  ];

  return (
    <AuthorizationGuard requiredRole="owner">
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
                      ? "bg-primary-100 text-primary font-medium"
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

"use client";

import { AuthorizationGuard } from "@/components/auth/AuthorizationGuard";
import { JourneyGateBoundary } from "@/components/journey/JourneyGateBoundary";
import { AppShellWrapper, Sidebar } from "@/components/app-shell";
import { Routes } from "@/app/routes";
import { useLocalizedText } from "@/localization";

export default function ClinicLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const { t } = useLocalizedText();

  const sidebarLinks = [
    { href: Routes.Clinic.Index, label: t("clinic.dashboard.title"), icon: "fa-solid fa-gauge" },
    { href: Routes.Clinic.Patients.Index, label: t("clinic.patients.title"), icon: "fa-solid fa-users" },
    { href: Routes.Clinic.CheckIn, label: t("clinic.dashboard.sections.checkIn.title"), icon: "fa-solid fa-clipboard-check" },
    { href: Routes.Clinic.Appointments.Reservations.Index, label: t("clinic.dashboard.sections.appointments.title"), icon: "fa-solid fa-calendar" },
    { href: Routes.Clinic.Reminders, label: t("clinic.dashboard.sections.reminders.title"), icon: "fa-solid fa-bell" },
    { href: Routes.Clinic.Settings.Index, label: t("clinic.dashboard.sections.settings.title"), icon: "fa-solid fa-gear" },
  ];

  return (
    <AuthorizationGuard requiredRole="owner">
      <AppShellWrapper
        sidebar={<Sidebar title={t("appShell.navigation.clinic")} links={sidebarLinks} />}
      >
        <div className="p-8">
          <JourneyGateBoundary>{children}</JourneyGateBoundary>
        </div>
      </AppShellWrapper>
    </AuthorizationGuard>
  );
}

"use client";

import { AuthorizationGuard } from "@/components/auth/AuthorizationGuard";
import { JourneyGateBoundary } from "@/components/journey/JourneyGateBoundary";
import { AppShellWrapper, Sidebar } from "@/components/app-shell";
import { Routes } from "@/app/routes";
import { useLocalizedText } from "@/localization";

export default function ClinicUserLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const { t } = useLocalizedText();

  const sidebarLinks = [
    { href: Routes.ClinicUser.Shifts.Index, label: t("clinic.userAppointments.shifts.title"), icon: "fa-solid fa-clock" },
    { href: Routes.ClinicUser.Reservations.Index, label: t("clinic.userAppointments.reservations.title"), icon: "fa-solid fa-calendar-check" },
  ];

  return (
    <AuthorizationGuard requiredRole="user">
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

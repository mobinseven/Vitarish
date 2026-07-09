"use client";

import { Routes } from "@/app/routes";
import { ClinicNavigationGrid, type ClinicNavigationItem } from "@/features/clinic/components/ClinicNavigationGrid";
import { useLocalizedText } from "@/localization";

export function AppointmentSettingsDashboard() {
  const { t } = useLocalizedText();

  const settingsLinks: ClinicNavigationItem[] = [
    { title: t("clinic.serviceTypes.title"), description: t("clinic.serviceTypes.description"), href: Routes.Clinic.Appointments.ServiceTypes.Index, icon: "fa-solid fa-tags" },
    { title: t("clinic.services.title"), description: t("clinic.services.description"), href: Routes.Clinic.Appointments.Services.Index, icon: "fa-solid fa-concierge-bell" },
    { title: t("clinic.serviceProviders.title"), description: t("clinic.serviceProviders.description"), href: Routes.Clinic.Appointments.ServiceProviders.Index, icon: "fa-solid fa-user-doctor" },
  ];

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold">{t("clinic.appointments.settings.title")}</h1>
        <p className="text-sm text-muted">{t("clinic.appointments.settings.description")}</p>
      </div>

      <ClinicNavigationGrid items={settingsLinks} />
    </div>
  );
}

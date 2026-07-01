"use client";

import { Card } from "@heroui/react";
import Link from "next/link";
import { Routes } from "@/app/routes";
import { useLocalizedText } from "@/localization";

interface AppointmentSettingsLink {
  title: string;
  description: string;
  href: string;
  icon: string;
}

export function AppointmentSettingsDashboard() {
  const { t } = useLocalizedText();

  const settingsLinks: AppointmentSettingsLink[] = [
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

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        {settingsLinks.map((link) => (
          <Link key={link.href} href={link.href} className="block">
            <Card className="glass-card h-full transition duration-300 hover:-translate-y-1 hover:shadow-lg">
              <Card.Header className="flex flex-row items-center gap-3">
                <i className={`${link.icon} text-lg text-accent`} />
                <h2 className="text-lg font-semibold">{link.title}</h2>
              </Card.Header>
              <Card.Content>
                <p className="text-sm text-muted">{link.description}</p>
              </Card.Content>
            </Card>
          </Link>
        ))}
      </div>
    </div>
  );
}

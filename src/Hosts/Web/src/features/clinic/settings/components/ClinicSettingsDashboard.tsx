"use client";

import { Card } from "@heroui/react";
import Link from "next/link";
import { Routes } from "@/app/routes";
import { useLocalizedText } from "@/localization";

interface SettingsLink {
  title: string;
  description: string;
  href: string;
  icon: string;
}

export function ClinicSettingsDashboard() {
  const { t } = useLocalizedText();

  const settingsLinks: SettingsLink[] = [
    { title: t("clinic.settings.sections.users.title"), description: t("clinic.settings.sections.users.description"), href: Routes.Clinic.Settings.Users, icon: "fa-solid fa-user-gear" },
    { title: t("clinic.settings.sections.appointmentSettings.title"), description: t("clinic.settings.sections.appointmentSettings.description"), href: Routes.Clinic.Appointments.Settings, icon: "fa-solid fa-calendar-check" },
    { title: t("clinic.settings.sections.statistics.title"), description: t("clinic.settings.sections.statistics.description"), href: Routes.Clinic.Settings.Statistics, icon: "fa-solid fa-chart-line" },
  ];

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold">{t("clinic.settings.title")}</h1>
        <p className="text-sm text-default-500">{t("clinic.settings.description")}</p>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        {settingsLinks.map((link) => (
          <Link key={link.href} href={link.href} className="block">
            <Card className="h-full">
              <Card.Header className="flex flex-row items-center gap-3">
                <i className={`${link.icon} text-lg text-primary`} />
                <h2 className="text-lg font-semibold">{link.title}</h2>
              </Card.Header>
              <Card.Content>
                <p className="text-sm text-default-500">{link.description}</p>
              </Card.Content>
            </Card>
          </Link>
        ))}
      </div>
    </div>
  );
}

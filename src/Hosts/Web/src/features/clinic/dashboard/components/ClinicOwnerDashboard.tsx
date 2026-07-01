"use client";

import { Card } from "@heroui/react";
import Link from "next/link";
import { Routes } from "@/app/routes";
import { useLocalizedText } from "@/localization";

interface DashboardSection {
  title: string;
  description: string;
  href: string;
  icon: string;
}

export function ClinicOwnerDashboard() {
  const { t } = useLocalizedText();

  const sections: DashboardSection[] = [
    { title: t("clinic.dashboard.sections.patients.title"), description: t("clinic.dashboard.sections.patients.description"), href: Routes.Clinic.Patients.Index, icon: "fa-solid fa-users" },
    { title: t("clinic.dashboard.sections.checkIn.title"), description: t("clinic.dashboard.sections.checkIn.description"), href: Routes.Clinic.CheckIn, icon: "fa-solid fa-clipboard-check" },
    { title: t("clinic.dashboard.sections.appointments.title"), description: t("clinic.dashboard.sections.appointments.description"), href: Routes.Clinic.Appointments.Reservations.Index, icon: "fa-solid fa-calendar" },
    { title: t("clinic.dashboard.sections.reminders.title"), description: t("clinic.dashboard.sections.reminders.description"), href: Routes.Clinic.Reminders, icon: "fa-solid fa-bell" },
    { title: t("clinic.dashboard.sections.settings.title"), description: t("clinic.dashboard.sections.settings.description"), href: Routes.Clinic.Settings.Index, icon: "fa-solid fa-gear" },
    { title: t("clinic.dashboard.sections.statistics.title"), description: t("clinic.dashboard.sections.statistics.description"), href: Routes.Clinic.Settings.Statistics, icon: "fa-solid fa-chart-bar" },
  ];

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold">{t("clinic.dashboard.title")}</h1>
        <p className="text-sm text-default-500">{t("clinic.dashboard.description")}</p>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        {sections.map((section) => (
          <Link key={section.href} href={section.href} className="block">
            <Card className="h-full">
              <Card.Header className="flex flex-row items-center gap-3">
                <i className={`${section.icon} text-lg text-primary`} />
                <h2 className="text-lg font-semibold">{section.title}</h2>
              </Card.Header>
              <Card.Content>
                <p className="text-sm text-default-500">{section.description}</p>
              </Card.Content>
            </Card>
          </Link>
        ))}
      </div>
    </div>
  );
}

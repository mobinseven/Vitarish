"use client";

import { Card, Link } from "@heroui/react";
import { Routes } from "@/app/routes";

interface SettingsLink {
  title: string;
  description: string;
  href: string;
  icon: string;
}

const settingsLinks: SettingsLink[] = [
  {
    title: "Users",
    description: "Manage clinic staff accounts and permissions.",
    href: Routes.Clinic.Settings.Users,
    icon: "fa-solid fa-user-gear",
  },
  {
    title: "Appointment Settings",
    description: "Configure services, service types, and providers.",
    href: Routes.Clinic.Appointments.Settings,
    icon: "fa-solid fa-calendar-check",
  },
  {
    title: "Statistics",
    description: "View clinic performance metrics and analytics.",
    href: Routes.Clinic.Settings.Statistics,
    icon: "fa-solid fa-chart-line",
  },
];

export function ClinicSettingsDashboard() {
  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold">Clinic Settings</h1>
        <p className="text-sm text-muted">Configure your clinic settings</p>
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

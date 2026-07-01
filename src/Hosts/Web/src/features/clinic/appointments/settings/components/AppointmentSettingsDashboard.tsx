"use client";

import { Card, Link } from "@heroui/react";
import { Routes } from "@/app/routes";

interface AppointmentSettingsLink {
  title: string;
  description: string;
  href: string;
  icon: string;
}

const settingsLinks: AppointmentSettingsLink[] = [
  {
    title: "Service Types",
    description: "Manage service types offered by the clinic.",
    href: Routes.Clinic.Appointments.ServiceTypes.Index,
    icon: "fa-solid fa-tags",
  },
  {
    title: "Services",
    description: "Manage individual services and their configurations.",
    href: Routes.Clinic.Appointments.Services.Index,
    icon: "fa-solid fa-concierge-bell",
  },
  {
    title: "Service Providers",
    description: "Manage healthcare providers and their specialties.",
    href: Routes.Clinic.Appointments.ServiceProviders.Index,
    icon: "fa-solid fa-user-doctor",
  },
];

export function AppointmentSettingsDashboard() {
  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold">Appointment Settings</h1>
        <p className="text-sm text-muted">Configure services, providers, and appointment policies</p>
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

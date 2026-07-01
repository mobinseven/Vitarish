"use client";

import { Card, Link } from "@heroui/react";
import { Routes } from "@/app/routes";

interface DashboardSection {
  title: string;
  description: string;
  href: string;
  icon: string;
}

const sections: DashboardSection[] = [
  {
    title: "Patients",
    description: "View and manage patient records, profiles, and medical information.",
    href: Routes.Clinic.Patients.Index,
    icon: "fa-solid fa-users",
  },
  {
    title: "Check-in",
    description: "Register patients for appointments and manage check-in workflows.",
    href: Routes.Clinic.CheckIn,
    icon: "fa-solid fa-clipboard-check",
  },
  {
    title: "Appointments",
    description: "Manage reservations, shifts, services, and appointment settings.",
    href: Routes.Clinic.Appointments.Reservations.Index,
    icon: "fa-solid fa-calendar",
  },
  {
    title: "Reminders",
    description: "View and manage patient reminders and notifications.",
    href: Routes.Clinic.Reminders,
    icon: "fa-solid fa-bell",
  },
  {
    title: "Settings",
    description: "Configure clinic settings, users, and appointment policies.",
    href: Routes.Clinic.Settings.Index,
    icon: "fa-solid fa-gear",
  },
  {
    title: "Statistics",
    description: "View clinic performance metrics and analytics.",
    href: Routes.Clinic.Settings.Statistics,
    icon: "fa-solid fa-chart-bar",
  },
];

export function ClinicOwnerDashboard() {
  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold">Clinic Dashboard</h1>
        <p className="text-sm text-muted">Manage your clinic operations</p>
      </div>

      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
        {sections.map((section) => (
          <Link key={section.href} href={section.href} className="block">
            <Card className="glass-card h-full transition duration-300 hover:-translate-y-1 hover:shadow-lg">
              <Card.Header className="flex flex-row items-center gap-3">
                <i className={`${section.icon} text-lg text-accent`} />
                <h2 className="text-lg font-semibold">{section.title}</h2>
              </Card.Header>
              <Card.Content>
                <p className="text-sm text-muted">{section.description}</p>
              </Card.Content>
            </Card>
          </Link>
        ))}
      </div>
    </div>
  );
}

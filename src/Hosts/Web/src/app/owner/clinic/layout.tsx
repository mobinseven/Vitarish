"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { AuthorizationGuard } from "@/components/auth/AuthorizationGuard";
import { JourneyGateBoundary } from "@/components/journey/JourneyGateBoundary";
import { Routes } from "@/app/routes";

const navLinks = [
  { href: Routes.Clinic.Index, label: "Dashboard", icon: "fa-solid fa-gauge" },
  { href: Routes.Clinic.Patients.Index, label: "Patients", icon: "fa-solid fa-users" },
  { href: Routes.Clinic.CheckIn, label: "Check-in", icon: "fa-solid fa-clipboard-check" },
  { href: Routes.Clinic.Appointments.Reservations.Index, label: "Appointments", icon: "fa-solid fa-calendar" },
  { href: Routes.Clinic.Reminders, label: "Reminders", icon: "fa-solid fa-bell" },
  { href: Routes.Clinic.Settings.Index, label: "Settings", icon: "fa-solid fa-gear" },
];

export default function ClinicLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const pathname = usePathname();

  return (
    <AuthorizationGuard requiredRole="owner">
      <div className="flex min-h-screen">
        <aside className="sidebar">
          <h2 className="text-lg font-bold mb-4 px-3">Clinic</h2>
          <nav className="flex flex-col gap-1">
            {navLinks.map((link) => {
              const isActive =
                pathname === link.href || pathname.startsWith(`${link.href}/`);
              return (
                <Link
                  key={link.href}
                  href={link.href}
                  className={`sidebar-nav-item ${isActive ? "sidebar-nav-item-active" : ""}`}
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

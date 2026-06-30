"use client";

import { useEffect } from "react";
import { useRouter, usePathname } from "next/navigation";
import Link from "next/link";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import { JourneyGateBoundary } from "@/components/journey/JourneyGateBoundary";
import { Routes } from "@/app/routes";

const navLinks = [
  { href: Routes.Clinic.Patients.Index, label: "Patients", icon: "fa-solid fa-users" },
];

export default function ClinicLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const { isAuthenticated, isLoading } = useAuthSession();
  const router = useRouter();
  const pathname = usePathname();

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      router.push(Routes.Login.Index);
    }
  }, [isLoading, isAuthenticated, router]);

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <p className="text-default-500">Loading...</p>
      </div>
    );
  }

  if (!isAuthenticated) {
    return null;
  }

  return (
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
  );
}

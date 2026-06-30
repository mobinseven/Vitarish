"use client";

import { useEffect } from "react";
import { useRouter, usePathname } from "next/navigation";
import Link from "next/link";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import { JourneyGateBoundary } from "@/components/journey/JourneyGateBoundary";
import { Routes } from "@/app/routes";

const navLinks = [
  { href: Routes.Account.Index, label: "Settings", icon: "fa-solid fa-gear" },
  { href: Routes.Account.Name, label: "Name", icon: "fa-solid fa-person" },
  { href: Routes.Account.Email, label: "Email", icon: "fa-solid fa-envelope" },
  { href: Routes.Account.Phone, label: "Phone", icon: "fa-solid fa-mobile" },
  { href: Routes.Account.Password, label: "Password", icon: "fa-solid fa-lock" },
  { href: Routes.Account.Passkeys, label: "Passkeys", icon: "fa-solid fa-key" },
  { href: Routes.Account.Theme, label: "Theme", icon: "fa-solid fa-palette" },
  { href: Routes.Account.Culture, label: "Language", icon: "fa-solid fa-globe" },
];

export default function AccountLayout({
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
        <h2 className="text-lg font-bold mb-4 px-3">Account</h2>
        <nav className="flex flex-col gap-1">
          {navLinks.map((link) => {
            const isActive =
              link.href === Routes.Account.Index
                ? pathname === Routes.Account.Index
                : pathname.startsWith(link.href);
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

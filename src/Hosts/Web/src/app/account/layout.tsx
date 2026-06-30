"use client";

import { useEffect } from "react";
import { useRouter, usePathname } from "next/navigation";
import Link from "next/link";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import { JourneyGateBoundary } from "@/components/journey/JourneyGateBoundary";

const navLinks = [
  { href: "/account", label: "Settings", icon: "fa-solid fa-gear" },
  { href: "/account/name", label: "Name", icon: "fa-solid fa-person" },
  { href: "/account/email", label: "Email", icon: "fa-solid fa-envelope" },
  { href: "/account/phone", label: "Phone", icon: "fa-solid fa-mobile" },
  { href: "/account/password", label: "Password", icon: "fa-solid fa-lock" },
  { href: "/account/passkeys", label: "Passkeys", icon: "fa-solid fa-key" },
  { href: "/account/theme", label: "Theme", icon: "fa-solid fa-palette" },
  { href: "/account/culture", label: "Language", icon: "fa-solid fa-globe" },
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
      router.push("/login");
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
              link.href === "/account"
                ? pathname === "/account"
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

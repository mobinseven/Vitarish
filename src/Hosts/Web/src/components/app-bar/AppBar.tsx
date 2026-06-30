"use client";

import { Link } from "@heroui/react";
import { usePathname } from "next/navigation";
import { Routes } from "@/app/routes";
import { AuthGate } from "./AuthGate";

const EXCLUDED_PATHS = [Routes.Login.Index, Routes.Logout];

export function AppBar() {
  const pathname = usePathname();

  const isExcluded = EXCLUDED_PATHS.some(
    (path) => pathname === path || pathname.startsWith(path + "/"),
  );

  if (isExcluded) {
    return null;
  }

  return (
    <header className="ds-app-bar">
      <Link
        href={Routes.Home}
        className="text-base font-bold tracking-tight text-foreground"
      >
        Vitarish
      </Link>
      <AuthGate />
    </header>
  );
}

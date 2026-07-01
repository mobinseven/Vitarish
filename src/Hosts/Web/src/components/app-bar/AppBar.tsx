"use client";

import { Link } from "@heroui/react";
import { usePathname } from "next/navigation";
import { Routes } from "@/app/routes";
import { useLocalizedText } from "@/localization";
import { AuthGate } from "./AuthGate";

const EXCLUDED_PATHS = [Routes.Login.Index, Routes.Logout];

export function AppBar() {
  const pathname = usePathname();
  const { t } = useLocalizedText();

  const isExcluded = EXCLUDED_PATHS.some(
    (path) => pathname === path || pathname.startsWith(path + "/"),
  );

  if (isExcluded) {
    return null;
  }

  return (
    <nav aria-label="Main navigation" className="flex items-center justify-between px-4 py-3">
      <Link href={Routes.Home} className="text-base font-bold tracking-tight text-foreground">
        {t("appShell.brand")}
      </Link>
      <AuthGate />
    </nav>
  );
}

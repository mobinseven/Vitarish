"use client";

import { Link } from "@heroui/react";
import { Routes } from "@/app/routes";
import { AuthGate } from "./AuthGate";

export function AppBar() {
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

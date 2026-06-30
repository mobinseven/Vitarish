"use client";

import { Link } from "@heroui/react";
import { Routes } from "@/app/routes";
import { useAuthSession } from "@/hooks/auth/useAuthSession";
import { UserMenu } from "./UserMenu";

export function AuthGate() {
  const { isAuthenticated, isLoading } = useAuthSession();

  if (isLoading) {
    return <div className="h-8 w-8 animate-pulse rounded-full bg-current/10" />;
  }

  if (!isAuthenticated) {
    return (
      <Link
        href={Routes.Login.Index}
        className="inline-flex min-h-8 items-center rounded-full bg-accent px-4 text-sm font-medium text-accent-foreground transition hover:opacity-90"
      >
        Log In
      </Link>
    );
  }

  return <UserMenu />;
}

"use client";

import { Button, Link } from "@heroui/react";
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
      <Button
        as={Link}
        href={Routes.Login.Index}
        variant="tertiary"
        className="rounded-full px-4 text-sm font-medium"
      >
        Log In
      </Button>
    );
  }

  return <UserMenu />;
}

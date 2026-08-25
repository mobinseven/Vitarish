"use client";

import { useCallback } from "react";
import { useRouter } from "next/navigation";
import { AuthorizationGuard } from "@vitarish/auth";
import { Routes } from "@/app/routes";
import { LoadingScreen } from "@/components/loading/LoadingScreen";

export default function OwnerLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const router = useRouter();
  const navigate = useCallback((href: string) => router.push(href), [router]);

  return (
    <AuthorizationGuard
      requiredRole="owner"
      loginHref={Routes.Login.Index}
      onNavigate={navigate}
      loadingFallback={<LoadingScreen />}
    >
      {children}
    </AuthorizationGuard>
  );
}

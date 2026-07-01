"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useTenantAuthorization, type TenantRole } from "@/hooks/auth/useTenantAuthorization";
import { Routes } from "@/app/routes";

interface AuthorizationGuardProps {
  requiredRole?: TenantRole;
  children: React.ReactNode;
}

export function AuthorizationGuard({ requiredRole, children }: AuthorizationGuardProps) {
  const { isAuthenticated, isLoading, hasAccess } = useTenantAuthorization(requiredRole);
  const router = useRouter();

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

  if (!hasAccess) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <div className="text-center">
          <h1 className="text-2xl font-bold text-danger">Access Denied</h1>
          <p className="text-sm text-muted mt-2">
            You don't have permission to access this area.
          </p>
        </div>
      </div>
    );
  }

  return <>{children}</>;
}

"use client";

import { useEffect } from "react";
import { useTenantAuthorization, type TenantRole } from "../hooks/useTenantAuthorization";
import { useLocalizedText } from "@vitarish/web-localization";
import { ignoreAuthNavigation, type AuthNavigate } from "../hooks/navigation";

interface AuthorizationGuardProps {
  requiredRole?: TenantRole;
  loginHref: string;
  onNavigate?: AuthNavigate;
  children: React.ReactNode;
}

export function AuthorizationGuard({ requiredRole, loginHref, onNavigate = ignoreAuthNavigation, children }: AuthorizationGuardProps) {
  const { isAuthenticated, isLoading, hasAccess } = useTenantAuthorization(requiredRole);
  const { t } = useLocalizedText();
  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      onNavigate(loginHref);
    }
  }, [isLoading, isAuthenticated, loginHref, onNavigate]);

  if (isLoading) {
    return (
      <div className="flex min-h-screen items-center justify-center">
        <p className="text-muted">{t("auth.guard.loading")}</p>
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
          <h1 className="text-2xl font-bold text-danger">{t("auth.guard.accessDenied")}</h1>
          <p className="text-sm text-muted mt-2">
            {t("auth.guard.noPermission")}
          </p>
        </div>
      </div>
    );
  }

  return <>{children}</>;
}

"use client";

import { useEffect, type ReactNode } from "react";
import {
  useTenantAuthorization,
  type TenantRole,
} from "../hooks/useTenantAuthorization";
import { useLocalizedText } from "@vitarish/localization";
import { StandaloneCenteredPage } from "@vitarish/ui/layout";
import { ignoreAuthNavigation, type AuthNavigate } from "../hooks/navigation";

interface AuthorizationGuardProps {
  requiredRole?: TenantRole;
  loginHref: string;
  onNavigate?: AuthNavigate;
  loadingFallback?: ReactNode;
  children: React.ReactNode;
}

export function AuthorizationGuard({
  requiredRole,
  loginHref,
  onNavigate = ignoreAuthNavigation,
  loadingFallback,
  children,
}: AuthorizationGuardProps) {
  const { isAuthenticated, isLoading, hasAccess } =
    useTenantAuthorization(requiredRole);
  const { t } = useLocalizedText();
  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      onNavigate(loginHref);
    }
  }, [isLoading, isAuthenticated, loginHref, onNavigate]);

  if (isLoading) {
    if (loadingFallback) {
      return <>{loadingFallback}</>;
    }

    return (
      <StandaloneCenteredPage>
        <p className="text-muted">{t("auth.guard.loading")}</p>
      </StandaloneCenteredPage>
    );
  }

  if (!isAuthenticated) {
    return null;
  }

  if (!hasAccess) {
    return (
      <StandaloneCenteredPage>
        <div className="text-center">
          <h1 className="text-2xl font-bold text-danger">
            {t("auth.guard.accessDenied")}
          </h1>
          <p className="text-sm text-muted mt-2">
            {t("auth.guard.noPermission")}
          </p>
        </div>
      </StandaloneCenteredPage>
    );
  }

  return <>{children}</>;
}

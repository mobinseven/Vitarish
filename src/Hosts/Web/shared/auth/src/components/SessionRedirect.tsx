"use client";

import { useEffect, type ReactNode } from "react";
import { useAuthSession } from "../hooks/useAuthSession";
import {
  ignoreAuthNavigation,
  type AuthNavigate,
  type AuthReturnUrlSearchParams,
} from "../hooks/navigation";
import { StandaloneCenteredPage } from "@vitarish/ui/layout";

interface RedirectSessionState {
  isLoading: boolean;
  isAuthenticated: boolean;
}

interface SessionRedirectOptions {
  authenticatedHref?: string | null;
  anonymousHref?: string | null;
}

interface SessionRedirectProps extends SessionRedirectOptions {
  loadingLabel?: string;
  loadingFallback?: ReactNode;
  onNavigate?: AuthNavigate;
}

interface AuthenticatedRedirectBoundaryProps {
  children: ReactNode;
  authenticatedHref?: string | null;
  loadingLabel?: string;
  loadingFallback?: ReactNode;
  onNavigate?: AuthNavigate;
  returnUrlSearchParams?: AuthReturnUrlSearchParams;
}

export function resolveSessionRedirectTarget({
  isLoading,
  isAuthenticated,
  authenticatedHref = null,
  anonymousHref = null,
}: RedirectSessionState & SessionRedirectOptions): string | null {
  if (isLoading) {
    return null;
  }

  return isAuthenticated ? authenticatedHref : anonymousHref;
}

function SessionRedirectMessage({
  label,
  fallback,
}: {
  label: string;
  fallback?: ReactNode;
}) {
  if (fallback) {
    return <>{fallback}</>;
  }

  return (
    <StandaloneCenteredPage>
      <p className="text-muted">{label}</p>
    </StandaloneCenteredPage>
  );
}

export function SessionRedirect({
  authenticatedHref = null,
  anonymousHref = null,
  loadingLabel = "Loading...",
  loadingFallback,
  onNavigate = ignoreAuthNavigation,
}: SessionRedirectProps) {
  const { isAuthenticated, isLoading } = useAuthSession();
  const redirectTarget = resolveSessionRedirectTarget({
    isLoading,
    isAuthenticated,
    authenticatedHref,
    anonymousHref,
  });

  useEffect(() => {
    if (redirectTarget) {
      onNavigate(redirectTarget);
    }
  }, [redirectTarget, onNavigate]);

  return <SessionRedirectMessage label={loadingLabel} fallback={loadingFallback} />;
}

export function AuthenticatedRedirectBoundary({
  children,
  authenticatedHref,
  loadingLabel = "Loading...",
  loadingFallback,
  onNavigate = ignoreAuthNavigation,
  returnUrlSearchParams,
}: AuthenticatedRedirectBoundaryProps) {
  const { isAuthenticated, isLoading } = useAuthSession();

  const returnUrl =
    returnUrlSearchParams?.get("returnUrl") ||
    returnUrlSearchParams?.get("redirectTo");
  const safeReturnUrl =
    returnUrl && returnUrl.startsWith("/") && !returnUrl.startsWith("//")
      ? returnUrl
      : null;

  const redirectTarget = resolveSessionRedirectTarget({
    isLoading,
    isAuthenticated,
    authenticatedHref: safeReturnUrl || authenticatedHref,
    anonymousHref: null,
  });

  useEffect(() => {
    if (redirectTarget) {
      onNavigate(redirectTarget);
    }
  }, [redirectTarget, onNavigate]);

  if (isLoading || redirectTarget) {
    return <SessionRedirectMessage label={loadingLabel} fallback={loadingFallback} />;
  }

  return <>{children}</>;
}

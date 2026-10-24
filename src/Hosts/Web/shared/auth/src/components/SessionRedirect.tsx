"use client";

import { useEffect, useRef, type ReactNode } from "react";
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
  // Only a visitor who arrives signed in is sent away. Someone who signs in on this page is
  // sent on by the sign-in form, which knows the server's destination; a second navigation
  // from here would race it.
  const arrivedSignedIn = useRef<boolean | null>(null);
  if (!isLoading && arrivedSignedIn.current === null) {
    arrivedSignedIn.current = isAuthenticated;
  }

  const returnUrl =
    returnUrlSearchParams?.get("returnUrl") ||
    returnUrlSearchParams?.get("redirectTo");
  const safeReturnUrl =
    returnUrl && returnUrl.startsWith("/") && !returnUrl.startsWith("//")
      ? returnUrl
      : null;

  const redirectTarget = resolveSessionRedirectTarget({
    isLoading,
    isAuthenticated: isAuthenticated && arrivedSignedIn.current === true,
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

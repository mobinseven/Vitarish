"use client";

import { useEffect, type ReactNode } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { useAuthSession } from "../hooks/useAuthSession";

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
}

interface AuthenticatedRedirectBoundaryProps {
  children: ReactNode;
  authenticatedHref?: string | null;
  loadingLabel?: string;
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

function SessionRedirectMessage({ label }: { label: string }) {
  return (
    <main className="grid min-h-screen place-items-center">
      <p className="text-muted">{label}</p>
    </main>
  );
}

export function SessionRedirect({
  authenticatedHref = null,
  anonymousHref = null,
  loadingLabel = "Loading...",
}: SessionRedirectProps) {
  const router = useRouter();
  const { isAuthenticated, isLoading } = useAuthSession();
  const redirectTarget = resolveSessionRedirectTarget({
    isLoading,
    isAuthenticated,
    authenticatedHref,
    anonymousHref,
  });

  useEffect(() => {
    if (redirectTarget) {
      router.replace(redirectTarget);
    }
  }, [redirectTarget, router]);

  return <SessionRedirectMessage label={loadingLabel} />;
}

export function AuthenticatedRedirectBoundary({
  children,
  authenticatedHref,
  loadingLabel = "Loading...",
}: AuthenticatedRedirectBoundaryProps) {
  const router = useRouter();
  const searchParams = useSearchParams();
  const { isAuthenticated, isLoading } = useAuthSession();

  const returnUrl = searchParams.get("returnUrl") || searchParams.get("redirectTo");
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
      router.replace(redirectTarget);
    }
  }, [redirectTarget, router]);

  if (isLoading || redirectTarget) {
    return <SessionRedirectMessage label={loadingLabel} />;
  }

  return <>{children}</>;
}

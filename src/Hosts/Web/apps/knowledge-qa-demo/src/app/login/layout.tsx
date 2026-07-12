"use client";

import { Suspense, useCallback, type ReactNode } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { AuthenticatedRedirectBoundary } from "@vitarish/auth-web";
import { Routes } from "@/app/routes";
import { useLocalizedText } from "@/localization";

function LoginFallback() {
  const { t } = useLocalizedText();

  return <p className="text-muted">{t("common.states.loading")}</p>;
}

function LoginRedirectBoundary({ children }: { children: ReactNode }) {
  const router = useRouter();
  const searchParams = useSearchParams();
  const navigate = useCallback((href: string) => router.replace(href), [router]);

  return (
    <AuthenticatedRedirectBoundary
      authenticatedHref={Routes.KnowledgeQa}
      onNavigate={navigate}
      returnUrlSearchParams={searchParams}
    >
      {children}
    </AuthenticatedRedirectBoundary>
  );
}

export default function LoginLayout({ children }: { children: ReactNode }) {
  return (
    <Suspense fallback={<LoginFallback />}>
      <LoginRedirectBoundary>{children}</LoginRedirectBoundary>
    </Suspense>
  );
}

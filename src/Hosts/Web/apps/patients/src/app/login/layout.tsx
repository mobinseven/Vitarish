"use client";

import { Suspense, useCallback, type ReactNode } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { AuthenticatedRedirectBoundary } from "@vitarish/auth";
import { useLocalizedText } from "@/localization";
import { Routes } from "@/app/routes";
import { StandaloneCenteredPage } from "@vitarish/ui/layout";

function LoginRouteFallback() {
  const { t } = useLocalizedText();

  return (
    <StandaloneCenteredPage>
      <p className="text-muted">{t("common.states.loading")}</p>
    </StandaloneCenteredPage>
  );
}

export default function LoginLayout({ children }: { children: ReactNode }) {
  const router = useRouter();
  const searchParams = useSearchParams();
  const navigate = useCallback(
    (href: string) => router.replace(href),
    [router],
  );

  return (
    <Suspense fallback={<LoginRouteFallback />}>
      <AuthenticatedRedirectBoundary
        authenticatedHref={Routes.Home}
        onNavigate={navigate}
        returnUrlSearchParams={searchParams}
      >
        {children}
      </AuthenticatedRedirectBoundary>
    </Suspense>
  );
}

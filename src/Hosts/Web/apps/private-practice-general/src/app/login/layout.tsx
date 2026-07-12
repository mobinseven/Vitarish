"use client";

import { Suspense, useCallback, type ReactNode } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { AuthenticatedRedirectBoundary } from "@vitarish/auth-web";
import { useLocalizedText } from "@/localization";
import { Routes } from "@/app/routes";

function LoginRouteFallback() {
  const { t } = useLocalizedText();

  return (
    <main className="grid min-h-screen place-items-center">
      <p className="text-muted">{t("common.states.loading")}</p>
    </main>
  );
}

export default function LoginLayout({ children }: { children: ReactNode }) {
  const router = useRouter();
  const searchParams = useSearchParams();
  const navigate = useCallback((href: string) => router.replace(href), [router]);

  return (
    <main className="min-h-screen">
      <Suspense fallback={<LoginRouteFallback />}>
        <AuthenticatedRedirectBoundary authenticatedHref={Routes.Home} onNavigate={navigate} returnUrlSearchParams={searchParams}>{children}</AuthenticatedRedirectBoundary>
      </Suspense>
    </main>
  );
}

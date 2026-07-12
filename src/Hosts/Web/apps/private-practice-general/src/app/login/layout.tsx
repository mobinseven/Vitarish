"use client";

import { Suspense, type ReactNode } from "react";
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
  return (
    <main className="min-h-screen">
      <Suspense fallback={<LoginRouteFallback />}>
        <AuthenticatedRedirectBoundary authenticatedHref={Routes.Home}>{children}</AuthenticatedRedirectBoundary>
      </Suspense>
    </main>
  );
}

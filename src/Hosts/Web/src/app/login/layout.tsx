"use client";

import { Suspense, type ReactNode } from "react";
import { AuthenticatedRedirectBoundary } from "@/components/auth/SessionRedirect";
import { useLocalizedText } from "@/localization";

function LoginRouteFallback() {
  const { t } = useLocalizedText();

  return (
    <main className="grid min-h-screen place-items-center">
      <p className="text-default-500">{t("common.states.loading")}</p>
    </main>
  );
}

export default function LoginLayout({ children }: { children: ReactNode }) {
  return (
    <Suspense fallback={<LoginRouteFallback />}>
      <AuthenticatedRedirectBoundary>{children}</AuthenticatedRedirectBoundary>
    </Suspense>
  );
}

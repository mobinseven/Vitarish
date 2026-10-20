"use client";

import { Suspense, useCallback, type ReactNode } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { AuthenticatedRedirectBoundary } from "@vitarish/auth";
import { Routes } from "@/app/routes";
import { BrandedPageFrame } from "@/components/branding/BrandedPageFrame";
import { LoadingScreen } from "@/components/loading/LoadingScreen";

function LoginRouteFallback() {
  return <LoadingScreen />;
}

export default function LoginLayout({ children }: { children: ReactNode }) {
  const router = useRouter();
  const searchParams = useSearchParams();
  const navigate = useCallback(
    (href: string) => router.replace(href),
    [router],
  );

  return (
    <BrandedPageFrame>
      <Suspense fallback={<LoginRouteFallback />}>
        <AuthenticatedRedirectBoundary
          authenticatedHref={Routes.Home}
          onNavigate={navigate}
          loadingFallback={<LoadingScreen />}
          returnUrlSearchParams={searchParams}
        >
          {children}
        </AuthenticatedRedirectBoundary>
      </Suspense>
    </BrandedPageFrame>
  );
}

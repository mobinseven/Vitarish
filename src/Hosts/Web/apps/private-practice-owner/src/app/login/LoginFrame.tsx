"use client";

import { useCallback, type ReactNode } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import { SignInFrame } from "@vitarish/auth";
import { Routes } from "@/app/routes";
import { BrandedPageFrame } from "@/components/branding/BrandedPageFrame";
import { LoadingScreen } from "@/components/loading/LoadingScreen";

/** The clinic app's router, chrome and destinations around the shared sign-in pages. */
export function LoginFrame({
  children,
  continueOnSignInHost,
}: {
  children: ReactNode;
  continueOnSignInHost: boolean;
}) {
  const router = useRouter();
  const navigate = useCallback((href: string) => router.replace(href), [router]);
  return (
    <SignInFrame
      continueOnSignInHost={continueOnSignInHost}
      searchParams={useSearchParams()}
      onNavigate={navigate}
      homeHref={Routes.Home}
      continueHref={Routes.SignInContinue}
      frame={BrandedPageFrame}
      loadingFallback={<LoadingScreen />}
    >
      {children}
    </SignInFrame>
  );
}

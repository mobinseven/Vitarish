"use client";

import { useRouter } from "next/navigation";
import { useCallback } from "react";
import { navigationTransitionRuntime } from "@vitarish/ui/navigation-runtime";
import { SessionRedirect } from "@vitarish/auth";
import { useLocalizedText } from "@vitarish/localization";
import { Routes } from "@/app/routes";

/** The shared sign-in host serves only sign-in: a signed-in person continues home, anyone else signs in. */
export function SignInHostRoot() {
  const router = useRouter();
  const { t } = useLocalizedText();
  const navigate = useCallback((href: string) => {
    navigationTransitionRuntime()?.departing();
    router.replace(href);
  }, [router]);
  return (
    <SessionRedirect
      authenticatedHref={Routes.SignInContinue}
      anonymousHref={Routes.Login.Index}
      loadingLabel={t("journey.continue.opening")}
      onNavigate={navigate}
    />
  );
}

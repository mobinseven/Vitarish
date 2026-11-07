"use client";

import { useRouter } from "next/navigation";
import { SessionRedirect } from "@vitarish/auth";
import { useLocalizedText } from "@vitarish/localization";
import { Routes } from "@/app/routes";

/** The shared sign-in host serves only sign-in: a signed-in person continues home, anyone else signs in. */
export function SignInHostRoot() {
  const router = useRouter();
  const { t } = useLocalizedText();
  return (
    <SessionRedirect
      authenticatedHref={Routes.SignInContinue}
      anonymousHref={Routes.Login.Index}
      loadingLabel={t("journey.continue.opening")}
      onNavigate={router.replace}
    />
  );
}

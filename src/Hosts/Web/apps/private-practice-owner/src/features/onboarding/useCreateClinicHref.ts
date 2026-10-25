"use client";

import { useEffect, useState } from "react";
import { isOnboardingHost } from "@vitarish/journey/onboarding-host";

/** The onboarding link, unless the current page already is the onboarding host. */
export function useCreateClinicHref(onboardingUrl: string | null): string | null {
  const [href, setHref] = useState<string | null>(null);
  useEffect(() => {
    if (!onboardingUrl) return;
    if (!isOnboardingHost(window.location.host, onboardingUrl)) setHref(new URL(onboardingUrl).toString());
  }, [onboardingUrl]);
  return href;
}

"use client";

import { useEffect, useState } from "react";

/** The onboarding link, unless the current page already is the onboarding host. */
export function useCreateClinicHref(onboardingUrl: string | null): string | null {
  const [href, setHref] = useState<string | null>(null);
  useEffect(() => {
    if (!onboardingUrl) return;
    const target = new URL(onboardingUrl);
    if (target.host !== window.location.host) setHref(target.toString());
  }, [onboardingUrl]);
  return href;
}

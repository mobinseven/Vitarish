"use client";

import { StandaloneJourneyFrame } from "@/components/journey/StandaloneJourneyFrame";

/** Setup runs on the onboarding host, where a new account has no clinic yet, so there is no clinic shell. */
export default function SetupLayout({ children }: { children: React.ReactNode }) {
  return <StandaloneJourneyFrame>{children}</StandaloneJourneyFrame>;
}

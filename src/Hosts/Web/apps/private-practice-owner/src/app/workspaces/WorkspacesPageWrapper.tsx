"use client";

import { StandaloneJourneyFrame } from "@/components/journey/StandaloneJourneyFrame";

/** Clinic selection runs on the shared sign-in host, where no clinic is open yet, so there is no clinic shell. */
export function WorkspacesPageWrapper({ children }: { children: React.ReactNode }) {
  return <StandaloneJourneyFrame>{children}</StandaloneJourneyFrame>;
}

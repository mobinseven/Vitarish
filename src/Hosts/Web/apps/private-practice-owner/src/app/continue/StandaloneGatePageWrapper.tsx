import { StandaloneJourneyFrame } from "@/components/journey/StandaloneJourneyFrame";

/** Gives an auth-host gate route its standalone page shell. */
export function StandaloneGatePageWrapper({ children }: { children: React.ReactNode }) {
  return <StandaloneJourneyFrame>{children}</StandaloneJourneyFrame>;
}

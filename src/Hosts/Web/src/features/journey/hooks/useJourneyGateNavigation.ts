import { useRouter } from "next/navigation";
import { resolvePostLoginNavigation, useCurrentJourney } from "@/hooks/journey";

export function useJourneyGateNavigation() {
  const router = useRouter();
  const { data: decision } = useCurrentJourney();

  return {
    navigateToResolvedDestination: () => {
      router.push(resolvePostLoginNavigation(decision));
    },
  };
}

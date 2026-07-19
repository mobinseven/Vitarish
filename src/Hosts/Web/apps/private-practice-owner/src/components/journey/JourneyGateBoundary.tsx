"use client";

import { useEffect } from "react";
import { useRouter, usePathname } from "next/navigation";
import { useCurrentJourney } from "@/hooks/journey/useCurrentJourney";
import { JourneyErrorState, JourneyLoadingState } from "@vitarish/journey-web";

interface JourneyGateBoundaryProps {
  children: React.ReactNode;
}

export function JourneyGateBoundary({ children }: JourneyGateBoundaryProps) {
  const router = useRouter();
  const pathname = usePathname();
  const { data: decision, isLoading, error, refetch } = useCurrentJourney();

  useEffect(() => {
    if (isLoading || error || !decision) return;

    const { step } = decision;

    if (step.blockingGate) {
      const gateDestination = step.blockingGate.destinationUrl;

      if (pathname === gateDestination) {
        return;
      }

      router.push(gateDestination);
      return;
    }
  }, [decision, isLoading, error, router, pathname]);

  if (isLoading) {
    return <JourneyLoadingState />;
  }

  if (error) {
    return <JourneyErrorState onRetry={() => void refetch()} />;
  }

  if (decision?.step?.blockingGate) {
    const gateDestination = decision.step.blockingGate.destinationUrl;

    if (pathname === gateDestination) {
      return <>{children}</>;
    }

    return null;
  }

  return <>{children}</>;
}

"use client";

import { useEffect } from "react";
import { useRouter, usePathname } from "next/navigation";
import { useCurrentJourney } from "@/hooks/journey/useCurrentJourney";

interface JourneyGateBoundaryProps {
  children: React.ReactNode;
}

export function JourneyGateBoundary({ children }: JourneyGateBoundaryProps) {
  const router = useRouter();
  const pathname = usePathname();
  const { data: decision, isLoading, error } = useCurrentJourney();

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
    return (
      <div className="flex items-center justify-center min-h-screen">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-gray-900" />
      </div>
    );
  }

  if (error) {
    return <>{children}</>;
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

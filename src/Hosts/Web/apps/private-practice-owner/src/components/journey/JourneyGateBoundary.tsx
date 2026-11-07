"use client";

import { useEffect } from "react";
import { useRouter, usePathname, useSearchParams } from "next/navigation";
import { useCurrentJourney } from "@/hooks/journey/useCurrentJourney";
import { AuthenticationSearchParams } from "@vitarish/auth/routes";
import { Routes } from "@/app/routes";
import { JourneyErrorState, JourneyLoadingState } from "@vitarish/journey";

interface JourneyGateBoundaryProps {
  children: React.ReactNode;
  central?: boolean;
}

export function JourneyGateBoundary({ children, central = false }: JourneyGateBoundaryProps) {
  const router = useRouter();
  const pathname = usePathname();
  const search = useSearchParams();
  const requestedReturnUrl = search.get(AuthenticationSearchParams.ReturnUrl);
  const { data: decision, isLoading, error, refetch } = useCurrentJourney(requestedReturnUrl ?? (central ? pathname : undefined));

  useEffect(() => {
    if (isLoading || error || !decision) return;

    const { step } = decision;

    if (step.blockingGate) {
      const gatePath = step.blockingGate.destinationUrl;
      const gateDestination = central && requestedReturnUrl
        ? `${gatePath}?${AuthenticationSearchParams.ReturnUrl}=${encodeURIComponent(requestedReturnUrl)}` : gatePath;

      if (pathname === gatePath) {
        return;
      }

      router.push(gateDestination);
      return;
    } else if (central && requestedReturnUrl && step.destinationUrl !== pathname &&
      new Set<string>([Routes.Account.ProfileComplete, Routes.Account.ContactConfirm, Routes.Account.Consents]).has(pathname)) {
      router.replace(step.destinationUrl);
    }
  }, [decision, isLoading, error, router, pathname, central, requestedReturnUrl]);

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

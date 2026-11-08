"use client";

import { Suspense, useEffect } from "react";
import { useRouter, usePathname, useSearchParams } from "next/navigation";
import { isSafeRelativeReturnTo, signInReturnUrlQueryKey } from "@vitarish/journey/sign-in-host";
import { Routes } from "@/app/routes";
import { useCurrentJourney } from "@/hooks/journey/useCurrentJourney";
import { JourneyErrorState, JourneyLoadingState } from "@vitarish/journey";

interface JourneyGateBoundaryProps {
  children: React.ReactNode;
  central?: boolean;
}

export function JourneyGateBoundary({ children, central = false }: JourneyGateBoundaryProps) {
  return (
    <Suspense fallback={<JourneyLoadingState />}>
      <JourneyGateContent central={central}>{children}</JourneyGateContent>
    </Suspense>
  );
}

function matchesJourneyDestination(
  pathname: string | null,
  currentSearch: string,
  destinationUrl: string,
): boolean {
  const queryIndex = destinationUrl.indexOf("?");
  const destinationPath = queryIndex < 0
    ? destinationUrl
    : destinationUrl.slice(0, queryIndex);

  if (pathname !== destinationPath) return false;
  if (queryIndex < 0) return true;

  const currentParams = new URLSearchParams(currentSearch);
  const destinationParams = new URLSearchParams(destinationUrl.slice(queryIndex + 1));
  for (const [key, value] of destinationParams.entries()) {
    if (!currentParams.getAll(key).includes(value)) return false;
  }
  return true;
}

function JourneyGateContent({ children, central = false }: JourneyGateBoundaryProps) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const requestedReturnUrl = searchParams.get(signInReturnUrlQueryKey);
  const returnUrl = isSafeRelativeReturnTo(requestedReturnUrl)
    ? requestedReturnUrl
    : undefined;
  const currentSearch = searchParams.toString();
  const { data: decision, isLoading, error, refetch } = useCurrentJourney(central ? requestedReturnUrl ?? pathname ?? undefined : returnUrl);

  useEffect(() => {
    if (isLoading || error || !decision) return;

    const { step } = decision;

    if (step.blockingGate) {
      const gateDestination = step.blockingGate.destinationUrl;

      if (matchesJourneyDestination(pathname, currentSearch, gateDestination)) {
        return;
      }

      router.push(gateDestination);
      return;
    }
    if (central && requestedReturnUrl && step.destinationUrl !== pathname &&
        new Set<string>([Routes.Account.ProfileComplete, Routes.Account.ContactConfirm, Routes.Account.Consents]).has(pathname ?? ""))
      router.replace(step.destinationUrl);
  }, [decision, isLoading, error, router, pathname, currentSearch, central, requestedReturnUrl]);

  if (isLoading) {
    return <JourneyLoadingState />;
  }

  if (error) {
    return <JourneyErrorState onRetry={() => void refetch()} />;
  }

  if (decision?.step?.blockingGate) {
    const gateDestination = decision.step.blockingGate.destinationUrl;

    if (matchesJourneyDestination(pathname, currentSearch, gateDestination)) {
      return <>{children}</>;
    }

    return null;
  }

  return <>{children}</>;
}

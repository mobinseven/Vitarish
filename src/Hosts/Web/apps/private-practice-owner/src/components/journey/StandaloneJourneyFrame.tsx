"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuthSession } from "@vitarish/auth";
import { BrandedPageFrame } from "@/components/branding/BrandedPageFrame";
import { JourneyGateBoundary } from "@/components/journey/JourneyGateBoundary";
import { LoadingScreen } from "@/components/loading/LoadingScreen";
import { Routes } from "@/app/routes";
import { StandaloneCenteredPage } from "@vitarish/ui/layout";

/**
 * A signed-in journey page on a platform host where the person has no clinic open, such as
 * clinic setup on the onboarding host or clinic selection on the sign-in host, so it has no
 * clinic shell. Signed-out visitors go to sign-in.
 */
export function StandaloneJourneyFrame({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, isLoading } = useAuthSession();
  const router = useRouter();

  useEffect(() => {
    if (!isLoading && !isAuthenticated) {
      router.push(Routes.Login.Index);
    }
  }, [isLoading, isAuthenticated, router]);

  if (isLoading) {
    return <LoadingScreen />;
  }

  if (!isAuthenticated) {
    return null;
  }

  return (
    <BrandedPageFrame>
      <StandaloneCenteredPage width="reading">
        <JourneyGateBoundary>{children}</JourneyGateBoundary>
      </StandaloneCenteredPage>
    </BrandedPageFrame>
  );
}

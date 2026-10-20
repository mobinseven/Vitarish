"use client";

import { useEffect } from "react";
import { useRouter } from "next/navigation";
import { useAuthSession } from "@vitarish/auth";
import { JourneyGateBoundary } from "@/components/journey/JourneyGateBoundary";
import { LoadingScreen } from "@/components/loading/LoadingScreen";
import { Routes } from "@/app/routes";
import { StandaloneCenteredPage } from "@vitarish/ui/layout";

/** Setup runs on the onboarding host, where a new account has no clinic yet, so there is no clinic shell. */
export default function SetupLayout({ children }: { children: React.ReactNode }) {
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
    <StandaloneCenteredPage width="reading">
      <JourneyGateBoundary>{children}</JourneyGateBoundary>
    </StandaloneCenteredPage>
  );
}

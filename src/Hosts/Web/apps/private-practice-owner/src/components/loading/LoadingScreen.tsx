"use client";

import { useLocalizedText } from "@/localization";
import { StandaloneCenteredPage } from "@vitarish/ui/layout";
import { LoadingSpinner } from "./LoadingSpinner";

export function LoadingScreen() {
  const { t } = useLocalizedText();

  return (
    <StandaloneCenteredPage
      className="min-h-dvh bg-background"
      aria-label={t("common.states.loading")}
      role="status"
    >
      <LoadingSpinner />
    </StandaloneCenteredPage>
  );
}

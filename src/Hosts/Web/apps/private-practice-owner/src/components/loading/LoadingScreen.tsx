"use client";

import { useLocalizedText } from "@/localization";
import { LoadingSpinner, StandaloneCenteredPage } from "@vitarish/ui";

export function LoadingScreen() {
  const { t } = useLocalizedText();

  return (
    <StandaloneCenteredPage
      className="bg-background"
      aria-label={t("common.states.loading")}
      role="status"
    >
      <LoadingSpinner />
    </StandaloneCenteredPage>
  );
}

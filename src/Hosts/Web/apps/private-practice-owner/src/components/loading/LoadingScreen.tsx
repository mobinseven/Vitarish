"use client";

import { useNavigationTransitionParticipant } from "@vitarish/ui/navigation-runtime";
import { useLocalizedText } from "@/localization";
import { LoadingSpinner, StandaloneCenteredPage } from "@vitarish/ui";

export function LoadingScreen() {
  const { t } = useLocalizedText();
  const covered = useNavigationTransitionParticipant(true);
  if (covered) return null;

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

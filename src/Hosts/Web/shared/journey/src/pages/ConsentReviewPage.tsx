"use client";

import { useLocalizedText } from "@vitarish/localization";
import { ConsentReviewPanel } from "../components/ConsentReviewPanel";
import { JourneyGateCard } from "../components/JourneyGateCard";
import { JourneyGateHeader } from "../components/JourneyGateHeader";
import { JourneyGateLayout } from "../components/JourneyGateLayout";

export function ConsentReviewPage() {
  const { t } = useLocalizedText();

  return (
    <JourneyGateLayout>
      <JourneyGateCard
        header={
          <JourneyGateHeader
            eyebrow={t("journey.gates.consent.eyebrow")}
            title={t("journey.gates.consent.title")}
            description={t("journey.gates.consent.description")}
          />
        }
      >
        <ConsentReviewPanel />
      </JourneyGateCard>
    </JourneyGateLayout>
  );
}

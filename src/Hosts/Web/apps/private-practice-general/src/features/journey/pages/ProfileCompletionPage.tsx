"use client";

import { useLocalizedText } from "@/localization";
import { JourneyGateCard } from "../components/JourneyGateCard";
import { JourneyGateHeader } from "../components/JourneyGateHeader";
import { JourneyGateLayout } from "../components/JourneyGateLayout";
import { ProfileCompletionForm } from "../components/ProfileCompletionForm";

export function ProfileCompletionPage() {
  const { t } = useLocalizedText();

  return (
    <JourneyGateLayout>
      <JourneyGateCard
        header={
          <JourneyGateHeader
            eyebrow={t("journey.gates.profile.eyebrow")}
            title={t("journey.gates.profile.title")}
            description={t("journey.gates.profile.description")}
          />
        }
      >
        <ProfileCompletionForm />
      </JourneyGateCard>
    </JourneyGateLayout>
  );
}

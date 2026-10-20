"use client";

import { useLocalizedText } from "@vitarish/localization";
import { JourneyGateCard } from "../components/JourneyGateCard";
import { JourneyGateHeader } from "../components/JourneyGateHeader";
import { JourneyGateLayout } from "../components/JourneyGateLayout";
import { ProfileCompletionForm } from "../components/ProfileCompletionForm";

interface ProfileCompletionPageProps {
  /** The signed-in user; undefined while the session is still loading. */
  userId: string | undefined;
}

export function ProfileCompletionPage({ userId }: ProfileCompletionPageProps) {
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
        <ProfileCompletionForm userId={userId} />
      </JourneyGateCard>
    </JourneyGateLayout>
  );
}

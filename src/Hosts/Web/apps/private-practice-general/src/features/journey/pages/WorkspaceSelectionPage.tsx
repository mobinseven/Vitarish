"use client";

import { useLocalizedText } from "@/localization";
import { JourneyGateCard } from "../components/JourneyGateCard";
import { JourneyGateHeader } from "../components/JourneyGateHeader";
import { JourneyGateLayout } from "../components/JourneyGateLayout";
import { WorkspaceSelector } from "../components/WorkspaceSelector";

export function WorkspaceSelectionPage() {
  const { t } = useLocalizedText();

  return (
    <JourneyGateLayout>
      <JourneyGateCard
        header={
          <JourneyGateHeader
            eyebrow={t("journey.gates.workspace.eyebrow")}
            title={t("journey.gates.workspace.title")}
            description={t("journey.gates.workspace.description")}
          />
        }
      >
        <WorkspaceSelector />
      </JourneyGateCard>
    </JourneyGateLayout>
  );
}

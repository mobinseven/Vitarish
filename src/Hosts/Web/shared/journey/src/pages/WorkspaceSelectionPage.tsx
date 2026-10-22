"use client";

import { useLocalizedText } from "@vitarish/localization";
import { JourneyGateCard } from "../components/JourneyGateCard";
import { JourneyGateHeader } from "../components/JourneyGateHeader";
import { JourneyGateLayout } from "../components/JourneyGateLayout";
import { ClinicSelector } from "../features/clinicSelection/ClinicSelector";

interface WorkspaceSelectionPageProps {
  /** The app route on each clinic and on the onboarding host that redeems the one-time sign-in token. */
  handoffPath: string;
}

/** The shared sign-in host's page for choosing which of the signed-in person's clinics to open. */
export function WorkspaceSelectionPage({ handoffPath }: WorkspaceSelectionPageProps) {
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
        <ClinicSelector handoffPath={handoffPath} />
      </JourneyGateCard>
    </JourneyGateLayout>
  );
}

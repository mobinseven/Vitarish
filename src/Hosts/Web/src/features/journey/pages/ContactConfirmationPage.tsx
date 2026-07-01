"use client";

import { useLocalizedText } from "@/localization";
import { ContactConfirmationPanel } from "../components/ContactConfirmationPanel";
import { JourneyGateCard } from "../components/JourneyGateCard";
import { JourneyGateHeader } from "../components/JourneyGateHeader";
import { JourneyGateLayout } from "../components/JourneyGateLayout";

export function ContactConfirmationPage() {
  const { t } = useLocalizedText();

  return (
    <JourneyGateLayout>
      <JourneyGateCard
        header={
          <JourneyGateHeader
            eyebrow={t("journey.gates.contact.eyebrow")}
            title={t("journey.gates.contact.title")}
            description={t("journey.gates.contact.description")}
          />
        }
      >
        <ContactConfirmationPanel />
      </JourneyGateCard>
    </JourneyGateLayout>
  );
}

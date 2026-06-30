import { ContactConfirmationPanel } from "../components/ContactConfirmationPanel";
import { JourneyGateCard } from "../components/JourneyGateCard";
import { JourneyGateHeader } from "../components/JourneyGateHeader";
import { JourneyGateLayout } from "../components/JourneyGateLayout";

export function ContactConfirmationPage() {
  return (
    <JourneyGateLayout>
      <JourneyGateCard
        header={
          <JourneyGateHeader
            eyebrow="Account security"
            title="Confirm your contact details"
            description="We need a verified way to reach you before you can continue."
          />
        }
      >
        <ContactConfirmationPanel />
      </JourneyGateCard>
    </JourneyGateLayout>
  );
}

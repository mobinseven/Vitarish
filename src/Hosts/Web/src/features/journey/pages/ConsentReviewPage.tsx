import { ConsentReviewPanel } from "../components/ConsentReviewPanel";
import { JourneyGateCard } from "../components/JourneyGateCard";
import { JourneyGateHeader } from "../components/JourneyGateHeader";
import { JourneyGateLayout } from "../components/JourneyGateLayout";

export function ConsentReviewPage() {
  return (
    <JourneyGateLayout>
      <JourneyGateCard
        header={
          <JourneyGateHeader
            eyebrow="Required review"
            title="Review required consents"
            description="Confirm you have reviewed the required terms and privacy notices before consent submission is enabled."
          />
        }
      >
        <ConsentReviewPanel />
      </JourneyGateCard>
    </JourneyGateLayout>
  );
}

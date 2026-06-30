import { JourneyGateCard } from "../components/JourneyGateCard";
import { JourneyGateHeader } from "../components/JourneyGateHeader";
import { JourneyGateLayout } from "../components/JourneyGateLayout";
import { ProfileCompletionForm } from "../components/ProfileCompletionForm";

export function ProfileCompletionPage() {
  return (
    <JourneyGateLayout>
      <JourneyGateCard
        header={
          <JourneyGateHeader
            eyebrow="Required step"
            title="Complete your profile"
            description="Add the profile details required before you continue to Vitarish."
          />
        }
      >
        <ProfileCompletionForm />
      </JourneyGateCard>
    </JourneyGateLayout>
  );
}

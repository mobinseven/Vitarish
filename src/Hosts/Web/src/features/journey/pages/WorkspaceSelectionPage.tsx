import { JourneyGateCard } from "../components/JourneyGateCard";
import { JourneyGateHeader } from "../components/JourneyGateHeader";
import { JourneyGateLayout } from "../components/JourneyGateLayout";
import { WorkspaceSelector } from "../components/WorkspaceSelector";

export function WorkspaceSelectionPage() {
  return (
    <JourneyGateLayout>
      <JourneyGateCard
        header={
          <JourneyGateHeader
            eyebrow="Workspace"
            title="Select a workspace"
            description="Choose the workspace or app context you want to enter."
          />
        }
      >
        <WorkspaceSelector />
      </JourneyGateCard>
    </JourneyGateLayout>
  );
}

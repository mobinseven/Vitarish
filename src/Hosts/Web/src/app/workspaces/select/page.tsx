import { WorkspaceSelectionPage } from "@/features/journey/pages/WorkspaceSelectionPage";
import { WorkspacesPageWrapper } from "../WorkspacesPageWrapper";

export default function WorkspaceSelectRoute() {
  return (
    <WorkspacesPageWrapper>
      <WorkspaceSelectionPage />
    </WorkspacesPageWrapper>
  );
}

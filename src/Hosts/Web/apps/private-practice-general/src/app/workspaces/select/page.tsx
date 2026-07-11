import { WorkspaceSelectionPage } from "@vitarish/journey-web";
import { WorkspacesPageWrapper } from "../WorkspacesPageWrapper";

export default function WorkspaceSelectRoute() {
  return (
    <WorkspacesPageWrapper>
      <WorkspaceSelectionPage />
    </WorkspacesPageWrapper>
  );
}

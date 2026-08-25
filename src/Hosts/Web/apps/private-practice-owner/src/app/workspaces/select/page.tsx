import { WorkspaceSelectionPage } from "@vitarish/journey";
import { WorkspacesPageWrapper } from "../WorkspacesPageWrapper";

export default function WorkspaceSelectRoute() {
  return (
    <WorkspacesPageWrapper>
      <WorkspaceSelectionPage />
    </WorkspacesPageWrapper>
  );
}

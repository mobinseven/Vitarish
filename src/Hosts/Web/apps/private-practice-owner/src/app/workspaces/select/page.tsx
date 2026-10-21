import { WorkspaceSelectionPage } from "@vitarish/journey";
import { Routes } from "@/app/routes";
import { WorkspacesPageWrapper } from "../WorkspacesPageWrapper";

export default function WorkspaceSelectRoute() {
  return (
    <WorkspacesPageWrapper>
      <WorkspaceSelectionPage handoffPath={Routes.Handoff} />
    </WorkspacesPageWrapper>
  );
}

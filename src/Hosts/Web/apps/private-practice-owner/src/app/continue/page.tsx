import { SignInContinuePage } from "@vitarish/journey";
import { WorkspacesPageWrapper } from "../workspaces/WorkspacesPageWrapper";

/** The shared sign-in host hands a signed-in person on to their platform home (ADR-0020). */
export default function SignInContinueRoute() {
  return (
    <WorkspacesPageWrapper>
      <SignInContinuePage />
    </WorkspacesPageWrapper>
  );
}

import { SignInContinuePage } from "@vitarish/journey";
import { Routes } from "@/app/routes";
import { StandaloneGatePageWrapper } from "./StandaloneGatePageWrapper";

/** The shared sign-in host hands a signed-in person on to their platform home (ADR-0020). */
export default function SignInContinueRoute() {
  return (
    <StandaloneGatePageWrapper>
      <SignInContinuePage signInPath={Routes.Login.Index} />
    </StandaloneGatePageWrapper>
  );
}

import { CentralAuthenticationContinue } from "@vitarish/auth/central";
import { AuthenticationSearchParams } from "@vitarish/auth/routes";
import { SignInContinuePage } from "@vitarish/journey";
import { Routes } from "@/app/routes";
import { StandaloneGatePageWrapper } from "./StandaloneGatePageWrapper";

export default async function SignInContinueRoute({ searchParams }: { searchParams: Promise<Record<string, string | string[] | undefined>> }) {
  if ((await searchParams)[AuthenticationSearchParams.Request]) return <CentralAuthenticationContinue />;
  return <StandaloneGatePageWrapper><SignInContinuePage signInPath={Routes.Login.Index} /></StandaloneGatePageWrapper>;
}

import { headers } from "next/headers";
import { redirect } from "next/navigation";
import { isOnboardingHost, readOnboardingUrl } from "@vitarish/journey/onboarding-host";
import { isSignInHost, readSignInUrl } from "@vitarish/journey/sign-in-host";
import { Routes } from "@/app/routes";
import { OwnerLandingPage } from "@/features/landing/OwnerLandingPage";
import { SignInHostRoot } from "@/features/sign-in-host/SignInHostRoot";

export default async function RootPage() {
  const host = (await headers()).get("host");
  const onboardingUrl = readOnboardingUrl(process.env);
  // The onboarding host exists to create a clinic, so its front door is setup; setup sends a
  // signed-out visitor to sign-in first, and the journey brings them back.
  if (onboardingUrl && isOnboardingHost(host, onboardingUrl)) redirect(Routes.Setup);
  const signInUrl = readSignInUrl(process.env);
  if (signInUrl && isSignInHost(signInUrl, host)) return <SignInHostRoot />;
  return <OwnerLandingPage onboardingUrl={onboardingUrl} />;
}

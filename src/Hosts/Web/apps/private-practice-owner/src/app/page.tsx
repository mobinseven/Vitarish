import { headers } from "next/headers";
import { redirect } from "next/navigation";
import { isOnboardingHost, readOnboardingUrl } from "@vitarish/journey/onboarding-host";
import { Routes } from "@/app/routes";
import { OwnerLandingPage } from "@/features/landing/OwnerLandingPage";

export default async function RootPage() {
  const onboardingUrl = readOnboardingUrl(process.env);
  // The onboarding host exists to create a clinic, so its front door is setup; setup sends a
  // signed-out visitor to sign-in first, and the journey brings them back.
  if (onboardingUrl && isOnboardingHost((await headers()).get("host"), onboardingUrl)) redirect(Routes.Setup);
  return <OwnerLandingPage onboardingUrl={onboardingUrl} />;
}

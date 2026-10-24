import { readOnboardingUrl } from "@vitarish/journey/onboarding-host";
import { OwnerLandingPage } from "@/features/landing/OwnerLandingPage";

export default function RootPage() {
  return <OwnerLandingPage onboardingUrl={readOnboardingUrl(process.env)} />;
}

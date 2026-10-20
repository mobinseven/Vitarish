import { readOnboardingUrl } from "@vitarish/journey/onboarding-host";
import { LandingPage } from "../features/landing/landing-page";

export default function RootPage() {
  return <LandingPage onboardingUrl={readOnboardingUrl(process.env)} />;
}

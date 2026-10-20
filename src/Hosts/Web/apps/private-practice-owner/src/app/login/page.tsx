import { readOnboardingUrl } from "@vitarish/journey/onboarding-host";
import { LoginMethodsPage } from "./LoginMethodsPage";

export default function LoginPage() {
  return <LoginMethodsPage onboardingUrl={readOnboardingUrl(process.env)} />;
}

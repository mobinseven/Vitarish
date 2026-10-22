import { readOnboardingUrl } from "@vitarish/journey/onboarding-host";
import { FrontDoorPage } from "../features/front-door/front-door-page";
import { readWhatsAppNumber } from "../features/front-door/whatsapp";

export default function RootPage() {
  return <FrontDoorPage onboardingUrl={readOnboardingUrl(process.env)} whatsappNumber={readWhatsAppNumber(process.env)} />;
}

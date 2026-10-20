import { notFound } from "next/navigation";
import { readOnboardingUrl } from "@vitarish/journey/onboarding-host";
import { LandingPage } from "../../features/landing/landing-page";

const guidedPages = new Set(["about", "portfolio", "contact", "explore"]);

export default async function GuidedLandingPage({ params }: { params: Promise<{ page: string }> }) {
  const { page } = await params;
  if (!guidedPages.has(page)) notFound();

  return <LandingPage onboardingUrl={readOnboardingUrl(process.env)} />;
}

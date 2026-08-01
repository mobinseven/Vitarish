import { notFound } from "next/navigation";
import { LandingPage } from "../../features/landing/landing-page";

const guidedPages = new Set(["about", "portfolio", "contact", "explore"]);

export default async function GuidedLandingPage({ params }: { params: Promise<{ page: string }> }) {
  const { page } = await params;
  if (!guidedPages.has(page)) notFound();

  return <LandingPage />;
}

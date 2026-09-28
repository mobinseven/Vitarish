import { notFound } from "next/navigation";
import { LandingPage } from "../../features/landing/landing-page";
import { GuidedRoutes } from "../routes";

const guidedPages = new Set(GuidedRoutes.map((route) => route.slice(1)));

export default async function GuidedLandingPage({ params }: { params: Promise<{ page: string }> }) {
  const { page } = await params;
  if (!guidedPages.has(page)) notFound();

  return <LandingPage />;
}

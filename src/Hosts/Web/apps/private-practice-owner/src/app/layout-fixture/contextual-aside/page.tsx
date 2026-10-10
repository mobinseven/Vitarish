import { requireTestFixtureRoutes } from "../../testFixtureRoutes";

import { ContextualAsideLayoutFixture } from "./ContextualAsideLayoutFixture";

export const dynamic = "force-dynamic";

interface ContextualAsideFixturePageProps {
  searchParams: Promise<{ aside?: string }>;
}

export default async function ContextualAsideFixturePage({
  searchParams,
}: ContextualAsideFixturePageProps) {
  requireTestFixtureRoutes();

  const { aside } = await searchParams;
  return <ContextualAsideLayoutFixture defaultAsideOpen={aside === "open"} />;
}

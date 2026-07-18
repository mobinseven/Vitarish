import { notFound } from "next/navigation";

import { ContextualAsideLayoutFixture } from "./ContextualAsideLayoutFixture";

export const dynamic = "force-dynamic";

interface ContextualAsideFixturePageProps {
  searchParams: Promise<{ aside?: string }>;
}

export default async function ContextualAsideFixturePage({
  searchParams,
}: ContextualAsideFixturePageProps) {
  if (process.env.VITARISH_LAYOUT_TEST_FIXTURES !== "1") {
    notFound();
  }

  const { aside } = await searchParams;
  return <ContextualAsideLayoutFixture defaultAsideOpen={aside === "open"} />;
}

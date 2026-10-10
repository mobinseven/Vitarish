import { notFound } from "next/navigation";

/** Serves test-only fixture routes only when the test servers enable them. */
export function requireTestFixtureRoutes(): void {
  if (process.env.VITARISH_LAYOUT_TEST_FIXTURES !== "1") {
    notFound();
  }
}

import { requireTestFixtureRoutes } from "../testFixtureRoutes";
import { ViewportRevealTestClient } from "./ViewportRevealTestClient";

export default function ViewportRevealTestPage() {
  requireTestFixtureRoutes();

  return <ViewportRevealTestClient />;
}

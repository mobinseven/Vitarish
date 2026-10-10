import { StandaloneDocumentPage } from "@vitarish/ui/layout";
import { requireTestFixtureRoutes } from "../testFixtureRoutes";
import { ColorLegend } from "./ColorLegend";

export default function ColorLegendPage() {
  requireTestFixtureRoutes();

  return (
    <StandaloneDocumentPage className="flex flex-col gap-6 p-6">
      <ColorLegend />
    </StandaloneDocumentPage>
  );
}

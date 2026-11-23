// @vitest-environment node

import { renderToString } from "react-dom/server";
import { expect, it } from "vitest";
import { PresentedSurface } from "../surfaces/PresentedSurface";

it("renders on the server without importing browser custom elements", () => {
  const html = renderToString(
    <PresentedSurface surface={{
      surfaceId: "server-render",
      protocol: "a2ui",
      protocolVersion: "1.0",
      catalogId: "test-catalog",
      components: [],
      dataModel: {},
    }} />,
  );

  expect(html).toBe("");
});

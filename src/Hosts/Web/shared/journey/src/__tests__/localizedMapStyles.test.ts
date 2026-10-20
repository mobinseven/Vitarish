import type { StyleSpecification } from "maplibre-gl";
import { describe, expect, it } from "vitest";
import {
  localizedNameExpression,
  localizeMapStyle,
} from "../features/tenantSetup/localizedMapStyles";

describe("localizedMapStyles", () => {
  it("prefers Persian name then local name for Persian locales", () => {
    expect(localizedNameExpression("fa")).toEqual([
      "coalesce",
      ["get", "name:fa"],
      ["get", "name"],
    ]);
    expect(localizedNameExpression("fa-IR")).toEqual([
      "coalesce",
      ["get", "name:fa"],
      ["get", "name"],
    ]);
  });

  it("prefers English and Latin name fallbacks before local name for English", () => {
    expect(localizedNameExpression("en")).toEqual([
      "coalesce",
      ["get", "name:en"],
      ["get", "name_en"],
      ["get", "name:latin"],
      ["get", "name"],
    ]);
  });

  it("rewrites symbol layers with name text-fields and leaves ref and non-symbol layers unchanged", () => {
    const roadShieldLayer: StyleSpecification["layers"][number] = {
      id: "highway-shield",
      type: "symbol",
      source: "openmaptiles",
      layout: { "text-field": ["get", "ref"] },
    };
    const backgroundLayer: StyleSpecification["layers"][number] = {
      id: "background",
      type: "background",
      paint: { "background-color": "#ffffff" },
    };
    const style: StyleSpecification = {
      version: 8,
      sources: {},
      layers: [
        backgroundLayer,
        {
          id: "place-city",
          type: "symbol",
          source: "openmaptiles",
          layout: { "text-field": ["get", "name"], "text-size": 14 },
        },
        {
          id: "poi-label",
          type: "symbol",
          source: "openmaptiles",
          layout: { "text-field": "{name_en}" },
        },
        roadShieldLayer,
      ],
    };

    const localized = localizeMapStyle(style, "fa");
    const expectedExpression = localizedNameExpression("fa");

    expect(localized.layers[0]).toBe(backgroundLayer);
    expect(localized.layers[1]).toEqual({
      id: "place-city",
      type: "symbol",
      source: "openmaptiles",
      layout: { "text-field": expectedExpression, "text-size": 14 },
    });
    expect(localized.layers[2]).toEqual({
      id: "poi-label",
      type: "symbol",
      source: "openmaptiles",
      layout: { "text-field": expectedExpression },
    });
    expect(localized.layers[3]).toBe(roadShieldLayer);
  });
});

import type { ExpressionSpecification, StyleSpecification } from "maplibre-gl";

/** OpenFreeMap's styles: OpenStreetMap data, no key. Production CSP must allow this host. */
export const mapStyleUrls = {
  light: "https://tiles.openfreemap.org/styles/positron",
  dark: "https://tiles.openfreemap.org/styles/dark",
} as const;

/**
 * Place names in the reader's language, falling back to the local name. In Iran the local
 * name is already Persian; elsewhere English readers get English or Latin names.
 */
export function localizedNameExpression(locale: string): ExpressionSpecification {
  return locale.startsWith("fa")
    ? ["coalesce", ["get", "name:fa"], ["get", "name"]]
    : ["coalesce", ["get", "name:en"], ["get", "name_en"], ["get", "name:latin"], ["get", "name"]];
}

/** Rewrites every place-name label of a style to the reader's language; other labels are kept. */
export function localizeMapStyle(style: StyleSpecification, locale: string): StyleSpecification {
  const nameExpression = localizedNameExpression(locale);
  return {
    ...style,
    layers: style.layers.map((layer) => {
      if (layer.type !== "symbol" || !layer.layout?.["text-field"]) return layer;
      const textField = JSON.stringify(layer.layout["text-field"]);
      // Only labels that print a name (as ["get", "name…"] or "{name…}"); route numbers keep their own field.
      if (!/["{]name/.test(textField)) return layer;
      return { ...layer, layout: { ...layer.layout, "text-field": nameExpression } };
    }),
  };
}

export async function fetchLocalizedMapStyle(url: string, locale: string, signal?: AbortSignal): Promise<StyleSpecification> {
  const response = await fetch(url, { signal });
  if (!response.ok) throw new Error(`Map style request failed with ${response.status}.`);
  return localizeMapStyle((await response.json()) as StyleSpecification, locale);
}

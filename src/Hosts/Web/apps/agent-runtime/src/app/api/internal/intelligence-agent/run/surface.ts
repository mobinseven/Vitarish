import { InMemoryCatalogProvider, validatePayload } from "@a2ui/agent";

type LoadedCatalog = ReturnType<InMemoryCatalogProvider["load"]>;

const catalogs = new Map<string, { id: string; version: string; catalog: LoadedCatalog }>();

function load(catalogJson: string) {
  let loaded = catalogs.get(catalogJson);
  if (!loaded) {
    const document = JSON.parse(catalogJson) as { catalogId?: unknown; protocolVersion?: unknown };
    if (typeof document.catalogId !== "string" || typeof document.protocolVersion !== "string") {
      throw new Error("The surface catalog has no id or protocol version.");
    }
    loaded = {
      id: document.catalogId,
      version: document.protocolVersion,
      catalog: new InMemoryCatalogProvider(document as Record<string, unknown>).load(),
    };
    catalogs.set(catalogJson, loaded);
  }
  return loaded;
}

/**
 * Validates a presented surface (ADR-0022 decision 8) against the catalog the tool definition carries.
 * Returns the problem to report to the model, or null when the surface is valid.
 */
export function surfaceProblem(catalogJson: string, argumentsValue: unknown): string | null {
  const surface = typeof argumentsValue === "object" && argumentsValue !== null
    ? (argumentsValue as { surface?: unknown }).surface
    : undefined;
  const components = typeof surface === "object" && surface !== null
    ? (surface as { components?: unknown }).components
    : undefined;
  if (!Array.isArray(components) || components.length === 0) {
    return "The surface must be {\"components\": [...]} with at least one component.";
  }
  const { id, version, catalog } = load(catalogJson);
  try {
    validatePayload([catalog], [
      { version, createSurface: { surfaceId: "surface", catalogId: id } },
      { version, updateComponents: { surfaceId: "surface", components } },
    ]);
    return null;
  } catch (error) {
    return `The surface is not valid: ${error instanceof Error ? error.message : String(error)}`;
  }
}

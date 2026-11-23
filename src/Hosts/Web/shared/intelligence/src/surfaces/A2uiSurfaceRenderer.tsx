"use client";

import { useMemo } from "react";
import { MessageProcessor } from "@a2ui/web_core";
import { A2uiSurface } from "@a2ui/react";
import type { GuidedSurfaceSurfaceResponse } from "../api/generated/intelligence/model";
import { presentedSurfaceCatalog, presentedSurfaceCatalogId } from "./surfaceCatalog";

function buildSurface(surface: GuidedSurfaceSurfaceResponse) {
  if (surface.catalogId !== presentedSurfaceCatalogId) {
    console.error(`Presented surface ${surface.surfaceId} uses an unknown catalog.`, surface.catalogId);
    return null;
  }
  const version = surface.protocolVersion;
  try {
    const processor = new MessageProcessor([presentedSurfaceCatalog()]);
    processor.processMessages([
      { version, createSurface: { surfaceId: surface.surfaceId, catalogId: surface.catalogId } },
      { version, updateComponents: { surfaceId: surface.surfaceId, components: surface.components } },
      { version, updateDataModel: { surfaceId: surface.surfaceId, path: "/", value: surface.dataModel } },
    ] as never);
    const model = processor.model.surfacesMap.get(surface.surfaceId) ?? null;
    if (!model) console.error(`Presented surface ${surface.surfaceId} was not created.`);
    return model;
  } catch (error) {
    console.error(`Presented surface ${surface.surfaceId} was rejected.`, error);
    return null;
  }
}

export default function A2uiSurfaceRenderer({ surface }: { surface: GuidedSurfaceSurfaceResponse }) {
  const model = useMemo(() => buildSurface(surface), [surface]);
  if (!model) return null;

  return (
    <div className="w-full">
      <A2uiSurface surface={model as never} />
    </div>
  );
}

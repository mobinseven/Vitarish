"use client";

import { Component, useMemo, type ReactNode } from "react";
import { MessageProcessor } from "@a2ui/web_core";
import { A2uiSurface } from "@a2ui/react";
import type { GuidedSurfaceSurfaceResponse } from "../api/generated/intelligence/model";
import { presentedSurfaceCatalog, presentedSurfaceCatalogId } from "./surfaceCatalog";

/** A surface that fails to render is dropped; the reply text beside it still answers. */
class SurfaceBoundary extends Component<{ children: ReactNode }, { failed: boolean }> {
  override state = { failed: false };

  static getDerivedStateFromError() {
    return { failed: true };
  }

  override render() {
    return this.state.failed ? null : this.props.children;
  }
}

function buildSurface(surface: GuidedSurfaceSurfaceResponse) {
  if (surface.catalogId !== presentedSurfaceCatalogId) return null;
  const version = surface.protocolVersion;
  try {
    const processor = new MessageProcessor([presentedSurfaceCatalog()]);
    processor.processMessages([
      { version, createSurface: { surfaceId: surface.surfaceId, catalogId: surface.catalogId } },
      { version, updateComponents: { surfaceId: surface.surfaceId, components: surface.components } },
      { version, updateDataModel: { surfaceId: surface.surfaceId, path: "/", value: surface.dataModel } },
    ] as never);
    return processor.model.surfacesMap.get(surface.surfaceId) ?? null;
  } catch {
    return null;
  }
}

/**
 * Renders one presented surface (ADR-0022 decision 8): the model's layout from the Vitarish catalog, filled with
 * the data the server read in that turn. Unknown protocols, catalogs or malformed surfaces render nothing.
 */
export function PresentedSurface({ surface }: { surface: GuidedSurfaceSurfaceResponse }) {
  const model = useMemo(() => buildSurface(surface), [surface]);
  if (!model) return null;
  return (
    <SurfaceBoundary>
      <div className="w-full">
        <A2uiSurface surface={model as never} />
      </div>
    </SurfaceBoundary>
  );
}

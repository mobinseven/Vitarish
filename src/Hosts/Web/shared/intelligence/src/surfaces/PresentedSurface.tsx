"use client";

import { Component, lazy, Suspense, useEffect, useState, type ReactNode } from "react";
import type { GuidedSurfaceSurfaceResponse } from "../api/generated/intelligence/model";

const A2uiSurfaceRenderer = lazy(() => import("./A2uiSurfaceRenderer"));

/** A surface that fails to render is dropped and reported; the reply text beside it still answers. */
class SurfaceBoundary extends Component<{ surfaceId: string; children: ReactNode }, { failed: boolean }> {
  override state = { failed: false };

  static getDerivedStateFromError() {
    return { failed: true };
  }

  override componentDidCatch(error: unknown) {
    console.error(`Presented surface ${this.props.surfaceId} failed to render.`, error);
  }

  override render() {
    return this.state.failed ? null : this.props.children;
  }
}

/**
 * Renders one presented surface (ADR-0022 decision 8): the model's layout from the Vitarish catalog, filled with
 * the data the server read in that turn. Unknown protocols, catalogs or malformed surfaces render nothing.
 */
export function PresentedSurface({ surface }: { surface: GuidedSurfaceSurfaceResponse }) {
  // The A2UI package registers custom elements as soon as it is imported.
  const [mounted, setMounted] = useState(false);
  useEffect(() => setMounted(true), []);

  if (!mounted) return null;
  return (
    <SurfaceBoundary surfaceId={surface.surfaceId}>
      <Suspense fallback={null}>
        <A2uiSurfaceRenderer surface={surface} />
      </Suspense>
    </SurfaceBoundary>
  );
}

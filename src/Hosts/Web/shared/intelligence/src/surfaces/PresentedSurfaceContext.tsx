"use client";

import { createContext, useContext, type ReactNode } from "react";
import type { PresentedRecord } from "./presentedData";

/**
 * What the app that owns the record kinds supplies to presented surfaces: localized labels and how a record
 * opens. Intelligence renders the components; the app owns its kinds, their labels and their screens.
 */
export interface PresentedSurfaceAppearance {
  /** The localized label of a field of a record kind; falls back to the field name. */
  fieldLabel(kind: string, field: string): string;
  /** The localized label of a status value of a record kind. */
  statusLabel?(kind: string, value: string): string;
  /** The Font Awesome icon name for a record kind, without the `fa-` prefix. */
  kindIcon?(kind: string): string;
  /** Opens a record's screen; records of kinds it returns false for are shown without an action. */
  canOpen?(record: PresentedRecord): boolean;
  openRecord?(record: PresentedRecord): void;
  /** Opens the full screen behind a shortened list of the given kind. */
  showAll?(kind: string): void;
}

const fallbackAppearance: PresentedSurfaceAppearance = { fieldLabel: (_kind, field) => field };

const PresentedSurfaceAppearanceContext = createContext<PresentedSurfaceAppearance>(fallbackAppearance);

export function PresentedSurfaceAppearanceProvider({ value, children }: { value: PresentedSurfaceAppearance; children: ReactNode }) {
  return <PresentedSurfaceAppearanceContext.Provider value={value}>{children}</PresentedSurfaceAppearanceContext.Provider>;
}

export function usePresentedSurfaceAppearance(): PresentedSurfaceAppearance {
  return useContext(PresentedSurfaceAppearanceContext);
}

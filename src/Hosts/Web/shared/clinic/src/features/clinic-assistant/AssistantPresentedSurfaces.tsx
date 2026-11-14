"use client";

import { useMemo } from "react";
import {
  PresentedSurface,
  PresentedSurfaceAppearanceProvider,
  type PresentedRecord,
  type GuidedSurfaceSurfaceResponse,
  type PresentedSurfaceAppearance,
} from "@vitarish/intelligence";
import { useLocalizedText } from "../../localization";
import { clinicPresentedKindIcons, clinicPresentedKinds } from "./presentedKinds";

export interface AssistantPresentedSurfacesProps {
  surfaces: readonly GuidedSurfaceSurfaceResponse[];
  /** Opens a patient the search presented; the reference is the run-scoped candidate token. */
  onSelectCandidate?: (candidateToken: string) => void;
}

/** True when a reply's surfaces already show its patient search, so the plain candidate list stays hidden. */
export function presentsPatientCandidates(surfaces: readonly GuidedSurfaceSurfaceResponse[]): boolean {
  return surfaces.some((surface) => {
    const data = surface.dataModel;
    return typeof data === "object" && data !== null &&
      Object.values(data as Record<string, unknown>).some((value) =>
        typeof value === "object" && value !== null && (value as { kind?: unknown }).kind === clinicPresentedKinds.patientCandidate);
  });
}

/** The Clinic assistant's presented surfaces (ADR-0022 decision 8), with Clinic's labels, icons and screens. */
export function AssistantPresentedSurfaces({ surfaces, onSelectCandidate }: AssistantPresentedSurfacesProps) {
  const { t } = useLocalizedText();
  const appearance = useMemo<PresentedSurfaceAppearance>(() => {
    const translated = (key: string, fallback: string) => {
      const text = t(key as never);
      return text === key ? fallback : text;
    };
    return {
      fieldLabel: (_kind, field) => translated(`clinic.assistantSurface.fields.${field}`, field),
      statusLabel: (kind, value) => kind === clinicPresentedKinds.setupGoal
        ? translated(`clinic.setup.goals.${value}`, value)
        : translated(`clinic.assistantSurface.statuses.${value}`, value),
      kindIcon: (kind) => clinicPresentedKindIcons[kind] ?? "file-lines",
      canOpen: (record: PresentedRecord) => record.kind === clinicPresentedKinds.patientCandidate && Boolean(onSelectCandidate),
      openRecord: (record: PresentedRecord) => {
        if (record.kind === clinicPresentedKinds.patientCandidate) onSelectCandidate?.(record.reference);
      },
    };
  }, [onSelectCandidate, t]);
  if (surfaces.length === 0) return null;
  return (
    <PresentedSurfaceAppearanceProvider value={appearance}>
      <div className="flex flex-col gap-4">
        {surfaces.map((surface) => <PresentedSurface key={surface.surfaceId} surface={surface} />)}
      </div>
    </PresentedSurfaceAppearanceProvider>
  );
}

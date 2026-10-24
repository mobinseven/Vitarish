"use client";

import { useQuery } from "@tanstack/react-query";
import {
  getLandingGetCatalogQueryOptions,
  LandingFeatureAvailability,
  type LandingCatalogResponse,
  type LandingFeatureResponse,
} from "@vitarish/platform";

/** The server-owned list of features, consultation questions and next steps the page and the assistant share. */
export function useLandingCatalog() {
  return useQuery(getLandingGetCatalogQueryOptions({ query: { staleTime: 60 * 60 * 1000 } }));
}

export type LandingCatalog = LandingCatalogResponse;

export type ChipTone = "success" | "warning" | "default";

/** Live features carry the success status, features being built the warning status, the rest stay neutral. */
export function availabilityTone(availability: LandingFeatureResponse["availability"]): ChipTone {
  switch (availability) {
    case LandingFeatureAvailability.Live:
      return "success";
    case LandingFeatureAvailability.InProgress:
      return "warning";
    default:
      return "default";
  }
}

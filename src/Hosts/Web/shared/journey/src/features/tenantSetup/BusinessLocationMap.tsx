"use client";

import { useQuery } from "@tanstack/react-query";
import { Map } from "@heroui-pro/react/map";
import { setWorkerUrl } from "maplibre-gl";
import { useLocalizedText } from "@vitarish/localization";
import { FaIcon } from "@vitarish/ui";
import { defaultBusinessLocation, type BusinessLocation } from "./businessDetailsForm";
import { fetchLocalizedMapStyle, mapStyleUrls } from "./localizedMapStyles";

// The host serves MapLibre's worker itself (see scripts/sync-maplibre-worker.mjs); Next inlines this at build.
// It must be set before the first map is created, so it is set when this module loads.
const workerUrl = process.env.NEXT_PUBLIC_MAPLIBRE_WORKER_URL;
if (workerUrl) setWorkerUrl(workerUrl);
const defaultZoom = 11;
const pinnedZoom = 15;

interface BusinessLocationMapProps {
  location: BusinessLocation | null;
  onLocationChange: (location: BusinessLocation) => void;
  onCenterChange: (center: BusinessLocation) => void;
}

/** The map part of the location picker; loaded only when the business details open. */
export default function BusinessLocationMap({ location, onLocationChange, onCenterChange }: BusinessLocationMapProps) {
  const { t, locale } = useLocalizedText();
  const styles = useQuery({
    queryKey: ["journey", "map-styles", locale],
    queryFn: async ({ signal }) => ({
      light: await fetchLocalizedMapStyle(mapStyleUrls.light, locale, signal),
      dark: await fetchLocalizedMapStyle(mapStyleUrls.dark, locale, signal),
    }),
    staleTime: Infinity,
    retry: 1,
  });

  if (styles.isError) {
    return (
      <div className="flex h-72 items-center justify-center rounded-2xl bg-default p-6 text-center text-base text-muted">
        {t("journey.setup.details.mapUnavailable")}
      </div>
    );
  }

  return (
    <Map
      aria-label={t("journey.setup.details.mapLabel")}
      className="h-72 w-full overflow-hidden rounded-2xl"
      center={[(location ?? defaultBusinessLocation).longitude, (location ?? defaultBusinessLocation).latitude]}
      zoom={location ? pinnedZoom : defaultZoom}
      isLoading={styles.isPending}
      styles={styles.data}
      onViewportChange={(viewport) => onCenterChange({ longitude: viewport.center[0], latitude: viewport.center[1] })}
    >
      {location ? (
        <Map.Marker
          draggable
          latitude={location.latitude}
          longitude={location.longitude}
          onDragEnd={({ lat, lng }) => onLocationChange({ latitude: lat, longitude: lng })}
        >
          <FaIcon icon="location-dot" size="2x" aria-hidden="true" className="text-accent drop-shadow" />
        </Map.Marker>
      ) : null}
      <Map.Controls>
        <Map.ZoomControl />
        <Map.LocateControl onLocate={(coordinates) => onLocationChange(coordinates)} />
      </Map.Controls>
    </Map>
  );
}

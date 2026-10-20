"use client";

import { lazy, Suspense, useState } from "react";
import { Button, Skeleton, Tooltip } from "@heroui/react";
import { useLocalizedText } from "@vitarish/localization";
import { FaIcon } from "@vitarish/ui";
import { defaultBusinessLocation, type BusinessLocation } from "./businessDetailsForm";

// MapLibre is large, so it loads only when someone opens the business details.
const BusinessLocationMap = lazy(() => import("./BusinessLocationMap"));

interface BusinessLocationPickerProps {
  location: BusinessLocation | null;
  onChange: (location: BusinessLocation | null) => void;
}

export function BusinessLocationPicker({ location, onChange }: BusinessLocationPickerProps) {
  const { t } = useLocalizedText();
  const [center, setCenter] = useState<BusinessLocation | null>(null);

  return (
    <div className="flex flex-col gap-3">
      <Suspense fallback={<Skeleton className="h-72 w-full rounded-2xl" />}>
        <BusinessLocationMap location={location} onLocationChange={onChange} onCenterChange={setCenter} />
      </Suspense>
      <p className="text-sm text-muted">
        {location ? t("journey.setup.details.pinPlacedHint") : t("journey.setup.details.pinHint")}
      </p>
      <div className="flex flex-wrap gap-2">
        {location ? (
          <Tooltip>
            <Button variant="tertiary" onPress={() => onChange(null)}>
              <FaIcon icon="location-pin-slash" aria-hidden="true" />
              {t("journey.setup.details.removePin")}
            </Button>
            <Tooltip.Content>{t("journey.setup.details.removePin")}</Tooltip.Content>
          </Tooltip>
        ) : (
          <Tooltip>
            <Button variant="secondary" onPress={() => onChange(center ?? defaultBusinessLocation)}>
              <FaIcon icon="location-dot" aria-hidden="true" />
              {t("journey.setup.details.placePin")}
            </Button>
            <Tooltip.Content>{t("journey.setup.details.placePin")}</Tooltip.Content>
          </Tooltip>
        )}
      </div>
    </div>
  );
}

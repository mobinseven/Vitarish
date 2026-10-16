"use client";

import { Chip } from "@heroui/react";
import { FaIcon } from "@vitarish/ui";
import { useLocalizedText, type MessageKey } from "../../localization";
import {
  surgeryStatuses,
  type SurgeryStatus,
} from "../../models/surgeries/surgeryModels";
import { surgeryStatusChipProps } from "./surgeryStatusChipProps";

const statusIcons: Record<SurgeryStatus, string> = {
  [surgeryStatuses.planned]: "calendar-clock",
  [surgeryStatuses.completed]: "circle-check",
  [surgeryStatuses.cancelled]: "calendar-xmark",
};

/** The localization key of each surgery status label. */
export const surgeryStatusLabelKeys: Record<SurgeryStatus, MessageKey> = {
  [surgeryStatuses.planned]: "clinic.surgeries.status.planned",
  [surgeryStatuses.completed]: "clinic.surgeries.status.completed",
  [surgeryStatuses.cancelled]: "clinic.surgeries.status.cancelled",
};

/** A soft chip with an icon and the localized label for a surgery status. */
export function SurgeryStatusChip({ status }: { status: SurgeryStatus }) {
  const { t } = useLocalizedText();

  return (
    <Chip size="sm" {...surgeryStatusChipProps(status)}>
      <FaIcon aria-hidden="true" icon={statusIcons[status]} />
      <Chip.Label>{t(surgeryStatusLabelKeys[status])}</Chip.Label>
    </Chip>
  );
}

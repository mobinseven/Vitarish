"use client";

import { Alert } from "@heroui/react";
import { formatDateTime, formatList, useLocalizedText } from "../../localization/index";
import {
  surgeryConflictReasons,
  type SurgeryConflict,
} from "../../models/surgeries/surgeryModels";

export interface SurgeryConflictAlertProps {
  /** Conflicts found for the currently entered plan. An empty list renders nothing. */
  conflicts: readonly SurgeryConflict[];
}

/**
 * Warns (never blocks) when the planned surgery overlaps another one at the
 * same location or with a shared team member.
 */
export function SurgeryConflictAlert({ conflicts }: SurgeryConflictAlertProps) {
  const { t, locale } = useLocalizedText();

  if (conflicts.length === 0) return null;

  const sharedMemberNames = (conflict: SurgeryConflict): string[] =>
    conflict.sharedTeamUserIds
      .map(
        (userId) =>
          conflict.surgery.team.find((member) => member.userId === userId)?.displayName,
      )
      .filter((name): name is string => Boolean(name));

  const reasonLabel = (conflict: SurgeryConflict, reason: SurgeryConflict["reasons"][number]): string => {
    if (reason === surgeryConflictReasons.sameLocation) {
      return t("clinic.surgeries.plan.conflictSameLocation");
    }
    const names = sharedMemberNames(conflict);
    return names.length > 0
      ? t("clinic.surgeries.plan.conflictSharedTeam", { names: formatList(names, locale) })
      : t("clinic.surgeries.plan.conflictSharedTeamUnknown");
  };

  return (
    <Alert status="warning">
      <Alert.Indicator />
      <Alert.Content>
        <Alert.Title>{t("clinic.surgeries.plan.conflictTitle")}</Alert.Title>
        <Alert.Description>
          {t("clinic.surgeries.plan.conflictDescription")}
        </Alert.Description>
        <ul className="mt-3 flex list-disc flex-col gap-3 ps-5">
          {conflicts.map((conflict) => (
            <li key={conflict.surgery.id} className="text-sm">
              <span className="font-medium text-foreground">
                {conflict.surgery.patientDisplayName ??
                  t("clinic.surgeries.fields.patient")}
              </span>
              {" — "}
              <span className="text-foreground">{conflict.surgery.name}</span>
              {" · "}
              <span className="text-muted">
                {formatDateTime(conflict.surgery.scheduledAt, locale)}
              </span>
              <span className="mt-1 block text-muted">
                {conflict.reasons
                  .map((reason) => reasonLabel(conflict, reason))
                  .join(" · ")}
              </span>
            </li>
          ))}
        </ul>
      </Alert.Content>
    </Alert>
  );
}

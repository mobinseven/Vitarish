"use client";

import { ItemCard } from "@heroui-pro/react";
import { Button, Tooltip } from "@heroui/react";
import { formatDate } from "@vitarish/localization";
import { FaIcon } from "@vitarish/ui";
import type { ClinicAssistantCandidateResponse } from "../../api/generated/clinic/model";
import { useLocalizedText } from "../../localization";

/** Patients the operations assistant found; choosing one continues the request about that patient. */
export function AssistantCandidateList({
  candidates,
  hasError,
  onSelect,
}: {
  candidates: readonly ClinicAssistantCandidateResponse[];
  hasError: boolean;
  onSelect: (candidateToken: string) => void;
}) {
  const { locale, t } = useLocalizedText();
  if (!candidates.length) return null;
  return (
    <div className="space-y-2 px-4" role="region" aria-label={t("clinic.operationsAssistant.candidates.label")}>
      {candidates.map((candidate) => <ItemCard key={candidate.candidateToken} variant="secondary">
        <ItemCard.Icon><FaIcon icon="user" aria-hidden="true" /></ItemCard.Icon>
        <ItemCard.Content><ItemCard.Title>{candidate.displayName}</ItemCard.Title><ItemCard.Description>{[candidate.maskedIdentification, candidate.maskedPhone, candidate.dateOfBirth ? formatDate(candidate.dateOfBirth, locale) : null].filter(Boolean).join(" · ")}</ItemCard.Description></ItemCard.Content>
        <ItemCard.Action><Tooltip><Button size="sm" variant="outline" aria-label={t("clinic.operationsAssistant.candidates.select")} onPress={() => onSelect(candidate.candidateToken)}><FaIcon icon="user-check" aria-hidden="true" />{t("clinic.operationsAssistant.candidates.select")}</Button><Tooltip.Content>{t("clinic.operationsAssistant.candidates.select")}</Tooltip.Content></Tooltip></ItemCard.Action>
      </ItemCard>)}
      {hasError ? <p role="alert" className="text-sm text-danger">{t("clinic.operationsAssistant.candidates.expired")}</p> : null}
    </div>
  );
}

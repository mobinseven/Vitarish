"use client";

import { Button, InputGroup, TextField, Tooltip } from "@heroui/react";
import { FaIcon } from "@vitarish/ui";
import type { NavigationIntent } from "../../../navigation";
import { useLocalizedText } from "../../../localization";
import type { Visit } from "../../../models/appointments/visitRows";
import { VisitList } from "./VisitList";

export interface VisitSearchFieldProps {
  value: string;
  onChange: (value: string) => void;
  className?: string;
}

export function VisitSearchField({ value, onChange, className }: VisitSearchFieldProps) {
  const { t } = useLocalizedText();
  return (
    <TextField aria-label={t("clinic.appointments.visits.searchLabel")} className={`min-w-0 ${className ?? ""}`}>
      <InputGroup className="h-10">
        <InputGroup.Prefix>
          <FaIcon aria-hidden="true" className="text-muted" icon="magnifying-glass" />
        </InputGroup.Prefix>
        <InputGroup.Input
          placeholder={t("clinic.appointments.visits.searchPlaceholder")}
          value={value}
          onChange={(event) => onChange(event.target.value)}
        />
      </InputGroup>
    </TextField>
  );
}

export interface VisitResultsProps {
  query: { isLoading: boolean; error: unknown; refetch: () => unknown };
  visits: Visit[];
  /** Shown when the period has visits but none match the search or filters. */
  isFiltered: boolean;
  emptyMessage: string;
  showDate?: boolean;
  onNavigate: (intent: NavigationIntent) => void;
}

/** The visit list, or its loading, failure, or empty state. */
export function VisitResults({ query, visits, isFiltered, emptyMessage, showDate, onNavigate }: VisitResultsProps) {
  const { t } = useLocalizedText();

  if (query.isLoading) {
    return (
      <p aria-live="polite" className="px-6 py-12 text-center text-sm text-muted">
        {t("clinic.appointments.visits.loading")}
      </p>
    );
  }

  if (query.error) {
    return (
      <div className="flex flex-col items-center gap-3 px-6 py-12 text-center" role="alert">
        <FaIcon aria-hidden="true" className="text-2xl text-danger" icon="triangle-exclamation" />
        <p className="text-sm text-danger">{t("clinic.appointments.visits.loadFailed")}</p>
        <Tooltip>
          <Button size="sm" variant="outline" onPress={() => void query.refetch()}>
            <FaIcon aria-hidden="true" icon="rotate-right" />
            {t("clinic.appointments.visits.retry")}
          </Button>
          <Tooltip.Content>{t("clinic.appointments.visits.retry")}</Tooltip.Content>
        </Tooltip>
      </div>
    );
  }

  if (visits.length === 0) {
    return (
      <div className="flex flex-col items-center gap-3 px-6 py-12 text-center">
        <FaIcon aria-hidden="true" className="text-2xl text-muted" icon="calendar-xmark" />
        <p className="text-sm text-muted">
          {isFiltered ? t("clinic.appointments.visits.noFilterResults") : emptyMessage}
        </p>
      </div>
    );
  }

  return <VisitList showDate={showDate} visits={visits} onNavigate={onNavigate} />;
}

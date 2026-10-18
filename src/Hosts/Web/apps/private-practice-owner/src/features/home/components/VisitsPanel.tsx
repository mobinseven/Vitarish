"use client";

import { useMemo, useState } from "react";
import { Button, Surface, Tooltip } from "@heroui/react";
import type { CalendarDate } from "@internationalized/date";
import {
  emptyVisitFilters,
  selectVisits,
  useClinicListDashboardAppointments,
  visitPeriodRange,
  VisitResults,
  VisitSearchField,
  type NavigationIntent,
} from "@vitarish/clinic";
import { FaIcon, HeaderIcon } from "@vitarish/ui";
import { formatNumber, useLocalizedText } from "@/localization";

export interface VisitsPanelProps {
  focusedDate: CalendarDate;
  onNavigate: (intent: NavigationIntent) => void;
}

/** The chosen day's visits in time order, searchable, with a way to every visit. */
export function VisitsPanel({ focusedDate, onNavigate }: VisitsPanelProps) {
  const { t, locale } = useLocalizedText();
  const [search, setSearch] = useState("");
  const range = useMemo(() => visitPeriodRange("day", focusedDate, locale), [focusedDate, locale]);
  const query = useClinicListDashboardAppointments(range);
  const visits = useMemo(() => query.data ?? [], [query.data]);
  const shown = useMemo(
    () => selectVisits(visits, { ...emptyVisitFilters, search }, "time", locale, t),
    [visits, search, locale, t],
  );

  return (
    <Surface className="flex flex-col gap-6 rounded-3xl p-6">
      <div className="flex items-center gap-3">
        <HeaderIcon icon="calendar-check" />
        <div className="min-w-0">
          <h2 className="text-lg font-semibold text-foreground">{t("clinic.appointments.visits.title")}</h2>
          <p className="text-sm text-muted">
            {t("clinic.appointments.visits.count", { count: formatNumber(shown.length, locale) })}
          </p>
        </div>
      </div>
      <VisitSearchField value={search} onChange={setSearch} />
      <div className="-mx-6 border-y border-separator">
        <VisitResults
          emptyMessage={t("clinic.appointments.visits.emptyDay")}
          isFiltered={visits.length > 0 && search.trim().length > 0}
          query={query}
          visits={shown}
          onNavigate={onNavigate}
        />
      </div>
      <div className="mt-auto flex justify-end">
        <Tooltip>
          <Button variant="tertiary" onPress={() => onNavigate({ kind: "ownerReservationIndex" })}>
            <FaIcon aria-hidden="true" icon="list" />
            {t("clinic.appointments.visits.seeAll")}
          </Button>
          <Tooltip.Content>{t("clinic.appointments.visits.seeAll")}</Tooltip.Content>
        </Tooltip>
      </div>
    </Surface>
  );
}

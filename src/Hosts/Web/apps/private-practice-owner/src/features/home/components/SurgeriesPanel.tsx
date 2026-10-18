"use client";

import { useMemo } from "react";
import { Button, Surface, Tooltip } from "@heroui/react";
import { getLocalTimeZone, toZoned, type CalendarDate } from "@internationalized/date";
import {
  surgeryDayKey,
  surgeryStatuses,
  SurgeryList,
  useCanPlanSurgeries,
  useSurgeryCalendar,
  type NavigationIntent,
  type Surgery,
} from "@vitarish/clinic";
import { FaIcon, HeaderIcon } from "@vitarish/ui";
import { dateValueToIsoDate, formatNumber, useLocalizedText } from "@/localization";

/** How far past the chosen day the panel looks for the next surgery: six calendar weeks. */
const nextSurgeryLookaheadDays = 42;

export interface SurgeriesPanelProps {
  focusedDate: CalendarDate;
  onNavigate: (intent: NavigationIntent) => void;
}

/** The chosen day's surgeries; on a day without any, the next planned one. */
export function SurgeriesPanel({ focusedDate, onNavigate }: SurgeriesPanelProps) {
  const { t, locale } = useLocalizedText();
  const canPlan = useCanPlanSurgeries();
  const timeZone = getLocalTimeZone();
  const range = useMemo(
    () => ({
      from: toZoned(focusedDate, timeZone).toAbsoluteString(),
      to: toZoned(focusedDate.add({ days: nextSurgeryLookaheadDays }), timeZone).toAbsoluteString(),
    }),
    [focusedDate, timeZone],
  );
  const query = useSurgeryCalendar(range);
  const { daySurgeries, nextSurgery } = useMemo(() => {
    const dayKey = dateValueToIsoDate(focusedDate);
    const ordered = [...(query.data ?? [])].sort((a, b) => a.scheduledAt.localeCompare(b.scheduledAt));
    return {
      daySurgeries: ordered.filter((surgery) => surgeryDayKey(surgery.scheduledAt, timeZone) === dayKey),
      nextSurgery: ordered.find(
        (surgery: Surgery) =>
          surgery.status === surgeryStatuses.planned && surgeryDayKey(surgery.scheduledAt, timeZone) > dayKey,
      ),
    };
  }, [query.data, focusedDate, timeZone]);
  const openSurgery = (surgery: Surgery) => onNavigate({ kind: "surgeryDetail", surgeryId: surgery.id });

  return (
    <Surface className="flex flex-col gap-6 rounded-3xl p-6">
      <div className="flex flex-col items-start gap-5 sm:flex-row sm:items-center sm:justify-between">
        <div className="flex items-center gap-3">
          <HeaderIcon icon="scissors" />
          <div className="min-w-0">
            <h2 className="text-lg font-semibold text-foreground">{t("clinic.surgeries.title")}</h2>
            <p className="text-sm text-muted">
              {t(
                daySurgeries.length === 1
                  ? "clinic.surgeries.calendar.periodCount.one"
                  : "clinic.surgeries.calendar.periodCount.other",
                { count: formatNumber(daySurgeries.length, locale) },
              )}
            </p>
          </div>
        </div>
        {canPlan ? (
          <Tooltip>
            <Button variant="primary" onPress={() => onNavigate({ kind: "surgeryNew" })}>
              <FaIcon aria-hidden="true" icon="plus" />
              {t("home.dashboard.addSurgery")}
            </Button>
            <Tooltip.Content>{t("home.dashboard.addSurgery")}</Tooltip.Content>
          </Tooltip>
        ) : null}
      </div>

      {query.isLoading ? (
        <p aria-live="polite" className="py-6 text-center text-sm text-muted">
          {t("home.dashboard.loadingSurgeries")}
        </p>
      ) : query.error ? (
        <div className="flex flex-col items-center gap-3 py-6 text-center" role="alert">
          <FaIcon aria-hidden="true" className="text-2xl text-danger" icon="triangle-exclamation" />
          <p className="text-sm text-danger">{t("home.dashboard.surgeriesLoadFailed")}</p>
          <Tooltip>
            <Button size="sm" variant="outline" onPress={() => void query.refetch()}>
              <FaIcon aria-hidden="true" icon="rotate-right" />
              {t("home.dashboard.retry")}
            </Button>
            <Tooltip.Content>{t("home.dashboard.retry")}</Tooltip.Content>
          </Tooltip>
        </div>
      ) : daySurgeries.length > 0 ? (
        <SurgeryList emptyMessage={t("clinic.surgeries.calendar.emptyDay")} surgeries={daySurgeries} onOpen={openSurgery} />
      ) : (
        <div className="flex flex-col gap-4">
          <p className="flex items-center gap-2 text-sm text-muted">
            <FaIcon aria-hidden="true" icon="calendar-xmark" />
            {t("clinic.surgeries.calendar.emptyDay")}
          </p>
          {nextSurgery ? (
            <div className="flex flex-col gap-2">
              <h3 className="text-sm font-medium text-muted">{t("home.dashboard.nextSurgery")}</h3>
              <SurgeryList
                showDate
                emptyMessage={t("home.dashboard.noSurgeries")}
                surgeries={[nextSurgery]}
                onOpen={openSurgery}
              />
            </div>
          ) : (
            <p className="text-sm text-muted">{t("home.dashboard.noSurgeries")}</p>
          )}
        </div>
      )}

      <div className="mt-auto flex justify-end">
        <Tooltip>
          <Button variant="tertiary" onPress={() => onNavigate({ kind: "surgeryIndex" })}>
            <FaIcon aria-hidden="true" icon="list" />
            {t("home.dashboard.allSurgeries")}
          </Button>
          <Tooltip.Content>{t("home.dashboard.allSurgeries")}</Tooltip.Content>
        </Tooltip>
      </div>
    </Surface>
  );
}

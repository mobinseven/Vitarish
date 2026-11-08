"use client";
import type { NavigationIntent } from "../../../navigation";
import { formatTimeOfDay, useLocalizedText } from "../../../localization/index";
import { EmptyStateSurface as ClinicEmptyState, ErrorStateSurface as ClinicErrorState, LoadingStateSurface as ClinicLoadingState, PageHeader } from "@vitarish/ui";
import { pageAncestors, usePageBack } from "../../navigation/usePageBack";
import { useOwnerShiftDetail } from "../../../hooks/appointments/shifts/useOwnerShifts";
export interface OwnerShiftOccurrencePageProps {
  shiftId: string;
  occurrenceDate: string;
  onNavigate: (intent: NavigationIntent) => void;
}
export function OwnerShiftOccurrencePage({
  shiftId,
  occurrenceDate,
  onNavigate,
}: OwnerShiftOccurrencePageProps) {
  const { t, locale } = useLocalizedText();
  const back = usePageBack([pageAncestors.settings, pageAncestors.shifts], onNavigate);
  const query = useOwnerShiftDetail(shiftId);
  if (query.isLoading)
    return <ClinicLoadingState label={t("common.states.loading")} />;
  if (query.error || !query.data)
    return (
      <ClinicErrorState title={t("clinic.appointments.shifts.notFound")} />
    );
  return (
    <section className="flex flex-col gap-6">
      <PageHeader
        icon="clock"
        title={`${formatTimeOfDay(query.data.startTime, locale)} - ${formatTimeOfDay(query.data.endTime, locale)}`}
        details={occurrenceDate}
        back={back}
      />
      <ClinicEmptyState
        title={t("clinic.appointments.shifts.occurrenceComingSoon")}
      />
    </section>
  );
}

"use client";
import { Button } from "@heroui/react";
import { DetailGrid } from "@vitarish/web-ui";
import type { ClinicNavigationIntent } from "../../../../../navigation";
import { formatDateTime, useLocalizedText, type MessageKey } from "../../../../../localization";
import { ErrorStateSurface as ClinicErrorState, LoadingStateSurface as ClinicLoadingState } from "@vitarish/web-ui";
import { useOwnerShiftDetail } from "../../../appointments/shifts/hooks/useOwnerShifts";
const dayKeys = ["clinic.weekdays.sunday","clinic.weekdays.monday","clinic.weekdays.tuesday","clinic.weekdays.wednesday","clinic.weekdays.thursday","clinic.weekdays.friday","clinic.weekdays.saturday"] satisfies MessageKey[];
export interface ClinicUserShiftDetailPageProps { shiftId: string; onNavigate: (intent: ClinicNavigationIntent) => void }
export function ClinicUserShiftDetailPage({ shiftId, onNavigate }: ClinicUserShiftDetailPageProps) {
  const { t, locale } = useLocalizedText(); const query = useOwnerShiftDetail(shiftId);
  if (query.isLoading) return <ClinicLoadingState label={t("common.states.loading")} />;
  if (query.error || !query.data) return <ClinicErrorState title={t("clinic.appointments.shifts.notFound")} />;
  const shift=query.data, day=t(dayKeys[shift.dayOfWeek] ?? "common.values.unknown");
  return <section className="flex flex-col gap-6"><header><Button variant="ghost" onPress={()=>onNavigate({kind:"userShiftIndex"})}>{t("common.actions.previous")}</Button><h1 className="text-2xl font-bold">{day} {shift.startTime} - {shift.endTime}</h1></header><DetailGrid items={[{label:t("clinic.fields.day"),value:day},{label:t("clinic.fields.start"),value:shift.startTime},{label:t("clinic.fields.end"),value:shift.endTime},{label:t("clinic.fields.availableSeats"),value:shift.totalSeats},{label:t("clinic.fields.timezone"),value:shift.timeZone},{label:t("clinic.fields.created"),value:formatDateTime(shift.createdAt,locale)}]} /></section>;
}

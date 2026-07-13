"use client";
import { useState } from "react";
import { Button } from "@heroui/react";
import { DetailGrid } from "@vitarish/web-ui";
import type { ClinicNavigationIntent } from "../../../../../navigation";
import { formatDateTime, useLocalizedText, type MessageKey } from "../../../../../localization";
import { ErrorStateSurface as ClinicErrorState, LoadingStateSurface as ClinicLoadingState } from "@vitarish/web-ui";
import { ShiftDeleteDialog } from "../components/ShiftDeleteDialog";
import { useDeleteShift, useOwnerShiftDetail } from "../hooks/useOwnerShifts";
const dayKeys = ["clinic.weekdays.sunday","clinic.weekdays.monday","clinic.weekdays.tuesday","clinic.weekdays.wednesday","clinic.weekdays.thursday","clinic.weekdays.friday","clinic.weekdays.saturday"] satisfies MessageKey[];
export interface ClinicOwnerShiftDetailPageProps { shiftId: string; onNavigate: (intent: ClinicNavigationIntent) => void }
export function ClinicOwnerShiftDetailPage({ shiftId, onNavigate }: ClinicOwnerShiftDetailPageProps) {
  const { t, locale } = useLocalizedText();
  const query = useOwnerShiftDetail(shiftId);
  const deletion = useDeleteShift();
  const [deleting, setDeleting] = useState(false);
  if (query.isLoading) return <ClinicLoadingState label={t("common.states.loading")} />;
  if (query.error || !query.data) return <ClinicErrorState title={t("clinic.appointments.shifts.notFound")} />;
  const shift = query.data;
  const day = t(dayKeys[shift.dayOfWeek] ?? "common.values.unknown");
  return <section className="flex flex-col gap-6" aria-labelledby="clinic-owner-shift-title">
    <header className="flex items-center justify-between"><div><Button variant="ghost" onPress={() => onNavigate({ kind: "ownerShiftIndex" })}>{t("common.actions.previous")}</Button><h1 id="clinic-owner-shift-title" className="text-2xl font-bold">{day} {shift.startTime} - {shift.endTime}</h1></div>
    <div className="flex gap-2"><Button variant="ghost" onPress={() => onNavigate({ kind: "ownerShiftEdit", shiftId })}>{t("common.actions.edit")}</Button><Button variant="danger" onPress={() => setDeleting(true)}>{t("common.actions.delete")}</Button></div></header>
    <DetailGrid items={[{label:t("clinic.fields.day"),value:day},{label:t("clinic.fields.start"),value:shift.startTime},{label:t("clinic.fields.end"),value:shift.endTime},{label:t("clinic.fields.totalSeats"),value:shift.totalSeats},{label:t("clinic.fields.timezone"),value:shift.timeZone},{label:t("clinic.fields.created"),value:formatDateTime(shift.createdAt,locale)}]} />
    {deletion.isError ? <ClinicErrorState title={t("clinic.appointments.shifts.deleteFailed")} /> : null}
    <ShiftDeleteDialog isOpen={deleting} isPending={deletion.isPending} onCancel={() => setDeleting(false)} onConfirm={() => deletion.mutate(shiftId,{onSuccess:()=>onNavigate({kind:"ownerShiftIndex"})})} />
  </section>;
}

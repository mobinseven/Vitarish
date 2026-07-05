"use client";

import { use } from "react";
import { useRouter } from "next/navigation";
import { Card, Button, Spinner } from "@heroui/react";
import { Routes } from "@/app/routes";
import { useOwnerShiftDetail, useDeleteShift } from "@/features/clinic/appointments/shifts/hooks/useOwnerShifts";
import { useLocalizedText, type MessageKey } from "@/localization";

const dayNameKeys = [
  "clinic.weekdays.sunday",
  "clinic.weekdays.monday",
  "clinic.weekdays.tuesday",
  "clinic.weekdays.wednesday",
  "clinic.weekdays.thursday",
  "clinic.weekdays.friday",
  "clinic.weekdays.saturday",
] satisfies MessageKey[];

export default function ClinicOwnerShiftDetailPage({
  params,
}: {
  params: Promise<{ shiftId: string }>;
}) {
  const { shiftId } = use(params);
  const router = useRouter();
  const { t } = useLocalizedText();
  const { data: shift, isLoading, error } = useOwnerShiftDetail(shiftId);
  const deleteShift = useDeleteShift();
  const dayName = shift ? t(dayNameKeys[shift.dayOfWeek]) : "";

  if (isLoading) {
    return (
      <Card className="p-8">
        <div className="flex items-center justify-center gap-2">
          <Spinner size="sm" />
          <span className="text-muted">{t("common.states.loading")}</span>
        </div>
      </Card>
    );
  }

  if (error || !shift) {
    return (
      <Card className="p-8">
        <p className="text-danger">{error?.message ?? t("clinic.appointments.shifts.notFound")}</p>
      </Card>
    );
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <div>
          <Button variant="ghost" className="mb-2" onPress={() => router.push(Routes.Clinic.Appointments.Shifts.Index)}>
            ← {t("common.actions.previous")}
          </Button>
          <h1 className="text-2xl font-bold">{dayName} {shift.startTime} - {shift.endTime}</h1>
        </div>
        <div className="flex gap-2">
          <Button variant="ghost" onPress={() => router.push(Routes.Clinic.Appointments.Shifts.Edit(shiftId))}>
            {t("common.actions.edit")}
          </Button>
          <Button
            variant="danger"
            isPending={deleteShift.isPending}
            onPress={() => {
              if (confirm(t("clinic.appointments.shifts.confirmDelete"))) {
                deleteShift.mutate(shiftId, {
                  onSuccess: () => router.push(Routes.Clinic.Appointments.Shifts.Index),
                });
              }
            }}
          >
            {t("common.actions.delete")}
          </Button>
        </div>
      </div>

      <Card>
        <Card.Content className="grid gap-4 sm:grid-cols-2">
          <DetailField label={t("clinic.fields.day")} value={dayName} />
          <DetailField label={t("clinic.fields.start")} value={shift.startTime} />
          <DetailField label={t("clinic.fields.end")} value={shift.endTime} />
          <DetailField label={t("clinic.fields.totalSeats")} value={String(shift.totalSeats)} />
          <DetailField label={t("clinic.fields.timezone")} value={shift.timeZone} />
          <DetailField label={t("clinic.fields.created")} value={new Date(shift.createdAt).toLocaleString()} />
        </Card.Content>
      </Card>
    </div>
  );
}

function DetailField({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex flex-col gap-1">
      <span className="text-xs text-muted">{label}</span>
      <span className="text-sm">{value}</span>
    </div>
  );
}

"use client";
import type { NavigationIntent } from "../../../navigation";
import { useLocalizedText } from "../../../localization/index";
import {
  ErrorStateSurface as ClinicErrorState,
  LoadingStateSurface as ClinicLoadingState,
  HeaderIcon,
} from "@vitarish/ui";
import { ShiftEditor } from "./ShiftEditor";
import {
  useOwnerShiftDetail,
  useUpdateShift,
} from "../../../hooks/appointments/shifts/useOwnerShifts";
export interface OwnerShiftEditPageProps {
  shiftId: string;
  onNavigate: (intent: NavigationIntent) => void;
}
export function OwnerShiftEditPage({
  shiftId,
  onNavigate,
}: OwnerShiftEditPageProps) {
  const { t } = useLocalizedText();
  const query = useOwnerShiftDetail(shiftId);
  const mutation = useUpdateShift();
  if (query.isLoading)
    return <ClinicLoadingState label={t("common.states.loading")} />;
  if (query.error || !query.data)
    return (
      <ClinicErrorState title={t("clinic.appointments.shifts.notFound")} />
    );
  const shift = query.data;
  return (
    <section className="flex flex-col gap-6">
      <div className="flex items-center gap-4">
        <HeaderIcon context="scheduling" icon="clock" level="page" />
        <div className="min-w-0">
          <h1 className="text-2xl font-bold">
            {t("clinic.appointments.shifts.editTitle")}
          </h1>
        </div>
      </div>
      <ShiftEditor
        initialValue={{
          dayOfWeek: shift.dayOfWeek,
          startTime: shift.startTime,
          endTime: shift.endTime,
          totalSeats: shift.totalSeats,
          timeZone: shift.timeZone,
        }}
        isPending={mutation.isPending}
        failure={
          mutation.isError
            ? t("clinic.appointments.shifts.saveFailed")
            : undefined
        }
        onCancel={() => onNavigate({ kind: "ownerShiftDetail", shiftId })}
        onSubmit={(value) =>
          mutation.mutate(
            { id: shiftId, ...value },
            {
              onSuccess: (result) => {
                if (result.succeeded)
                  onNavigate({ kind: "ownerShiftDetail", shiftId });
              },
            },
          )
        }
      />
    </section>
  );
}

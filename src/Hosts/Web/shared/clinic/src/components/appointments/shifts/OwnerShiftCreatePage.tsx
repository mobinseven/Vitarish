"use client";
import { getLocalTimeZone } from "@internationalized/date";
import type { NavigationIntent } from "../../../navigation";
import { useLocalizedText } from "../../../localization/index";
import { ShiftEditor } from "./ShiftEditor";
import { useCreateShift } from "../../../hooks/appointments/shifts/useOwnerShifts";
import { HeaderIcon } from "@vitarish/ui";
export interface OwnerShiftCreatePageProps {
  onNavigate: (intent: NavigationIntent) => void;
}
export function OwnerShiftCreatePage({
  onNavigate,
}: OwnerShiftCreatePageProps) {
  const { t } = useLocalizedText();
  const mutation = useCreateShift();
  return (
    <section className="flex flex-col gap-6">
      <header>
        <div className="flex items-center gap-4">
          <HeaderIcon context="scheduling" icon="clock" level="page" />
          <div className="min-w-0">
            <h1 className="text-2xl font-bold">
              {t("clinic.appointments.shifts.createTitle")}
            </h1>
          </div>
        </div>
      </header>
      <ShiftEditor
        initialValue={{
          dayOfWeek: 1,
          startTime: "09:00",
          endTime: "10:00",
          totalSeats: 10,
          timeZone: getLocalTimeZone(),
        }}
        isPending={mutation.isPending}
        failure={
          mutation.isError
            ? t("clinic.appointments.shifts.saveFailed")
            : undefined
        }
        onCancel={() => onNavigate({ kind: "ownerShiftIndex" })}
        onSubmit={(value) =>
          mutation.mutate(value, {
            onSuccess: (result) => {
              if (result.succeeded && result.entityId)
                onNavigate({
                  kind: "ownerShiftDetail",
                  shiftId: result.entityId,
                });
            },
          })
        }
      />
    </section>
  );
}

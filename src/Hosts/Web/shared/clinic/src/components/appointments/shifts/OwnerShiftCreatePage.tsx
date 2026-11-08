"use client";
import { getLocalTimeZone } from "@internationalized/date";
import type { NavigationIntent } from "../../../navigation";
import { useLocalizedText } from "../../../localization/index";
import { ShiftEditor } from "./ShiftEditor";
import { useCreateShift } from "../../../hooks/appointments/shifts/useOwnerShifts";
import { PageHeader } from "@vitarish/ui";
import { pageAncestors, usePageBack } from "../../navigation/usePageBack";
export interface OwnerShiftCreatePageProps {
  onNavigate: (intent: NavigationIntent) => void;
}
export function OwnerShiftCreatePage({
  onNavigate,
}: OwnerShiftCreatePageProps) {
  const { t } = useLocalizedText();
  const back = usePageBack([pageAncestors.settings, pageAncestors.shifts], onNavigate);
  const mutation = useCreateShift();
  return (
    <section className="flex flex-col gap-6">
      <PageHeader
        icon="clock"
        title={t("clinic.appointments.shifts.createTitle")}
        back={back}
      />
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

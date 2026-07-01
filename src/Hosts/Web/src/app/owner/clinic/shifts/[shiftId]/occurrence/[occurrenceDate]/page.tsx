"use client";

import { use } from "react";
import { useRouter } from "next/navigation";
import { Card, Button, Spinner } from "@heroui/react";
import { Routes } from "@/app/routes";
import { useOwnerShiftDetail } from "@/features/clinic/appointments/shifts/hooks/useOwnerShifts";
import { useLocalizedText } from "@/localization";

export default function ClinicOwnerShiftOccurrencePage({
  params,
}: {
  params: Promise<{ shiftId: string; occurrenceDate: string }>;
}) {
  const { shiftId, occurrenceDate } = use(params);
  const router = useRouter();
  const { t } = useLocalizedText();
  const { data: shift, isLoading } = useOwnerShiftDetail(shiftId);

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Button variant="ghost" className="mb-2" onPress={() => router.push(Routes.Clinic.Appointments.Shifts.Detail(shiftId))}>
          ← {t("common.actions.previous")}
        </Button>
        <h1 className="text-2xl font-bold">
          {shift ? `${shift.startTime} - ${shift.endTime}` : t("common.states.loading")}
        </h1>
        <p className="text-sm text-default-500">{occurrenceDate}</p>
      </div>

      {isLoading ? (
        <Card className="p-8">
          <div className="flex items-center justify-center gap-2">
            <Spinner size="sm" />
          </div>
        </Card>
      ) : (
        <Card>
          <Card.Content className="py-8 text-center">
            <p className="text-sm text-default-500">{t("clinic.appointments.shifts.occurrenceComingSoon")}</p>
          </Card.Content>
        </Card>
      )}
    </div>
  );
}

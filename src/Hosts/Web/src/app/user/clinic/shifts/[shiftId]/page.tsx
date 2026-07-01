"use client";

import { use } from "react";
import { useRouter } from "next/navigation";
import { Card, Button, Spinner } from "@heroui/react";
import { Routes } from "@/app/routes";
import { useOwnerShiftDetail } from "@/features/clinic/appointments/shifts/hooks/useOwnerShifts";
import { useLocalizedText } from "@/localization";

const DAY_NAMES = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];

export default function ClinicUserShiftDetailPage({
  params,
}: {
  params: Promise<{ shiftId: string }>;
}) {
  const { shiftId } = use(params);
  const router = useRouter();
  const { t } = useLocalizedText();
  const { data: shift, isLoading, error } = useOwnerShiftDetail(shiftId);

  if (isLoading) {
    return (
      <Card className="p-8">
        <div className="flex items-center justify-center gap-2">
          <Spinner size="sm" />
          <span className="text-default-500">{t("common.loading")}</span>
        </div>
      </Card>
    );
  }

  if (error || !shift) {
    return (
      <Card className="p-8">
        <p className="text-danger">{error?.message ?? "Shift not found."}</p>
      </Card>
    );
  }

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Button variant="ghost" className="mb-2" onPress={() => router.push(Routes.ClinicUser.Shifts.Index)}>
          ← {t("common.actions.back")}
        </Button>
        <h1 className="text-2xl font-bold">{DAY_NAMES[shift.dayOfWeek]} {shift.startTime} - {shift.endTime}</h1>
      </div>

      <Card>
        <Card.Content className="grid gap-4 sm:grid-cols-2">
          <DetailField label="Day" value={DAY_NAMES[shift.dayOfWeek]} />
          <DetailField label="Start" value={shift.startTime} />
          <DetailField label="End" value={shift.endTime} />
          <DetailField label="Available Seats" value={String(shift.totalSeats)} />
          <DetailField label="Timezone" value={shift.timeZone} />
          <DetailField label="Created" value={new Date(shift.createdAt).toLocaleString()} />
        </Card.Content>
      </Card>
    </div>
  );
}

function DetailField({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex flex-col gap-1">
      <span className="text-xs text-default-500">{label}</span>
      <span className="text-sm">{value}</span>
    </div>
  );
}

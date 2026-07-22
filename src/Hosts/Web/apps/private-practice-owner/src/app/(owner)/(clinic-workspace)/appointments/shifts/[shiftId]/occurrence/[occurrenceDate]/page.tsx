"use client";
import { use } from "react";
import { OwnerShiftOccurrencePage } from "@vitarish/clinic-web";
import { useClinicNavigation } from "@/components/clinic-navigation";
export default function OwnerShiftOccurrenceRoute({
  params,
}: {
  params: Promise<{ shiftId: string; occurrenceDate: string }>;
}) {
  const { shiftId, occurrenceDate } = use(params);
  return (
    <OwnerShiftOccurrencePage
      shiftId={shiftId}
      occurrenceDate={occurrenceDate}
      onNavigate={useClinicNavigation()}
    />
  );
}

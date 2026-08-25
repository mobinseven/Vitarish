"use client";
import { use } from "react";
import { OwnerShiftDetailPage } from "@vitarish/clinic";
import { useClinicNavigation } from "@/components/clinic-navigation";
export default function OwnerShiftDetailRoute({
  params,
}: {
  params: Promise<{ shiftId: string }>;
}) {
  const { shiftId } = use(params);
  return (
    <OwnerShiftDetailPage
      shiftId={shiftId}
      onNavigate={useClinicNavigation()}
    />
  );
}

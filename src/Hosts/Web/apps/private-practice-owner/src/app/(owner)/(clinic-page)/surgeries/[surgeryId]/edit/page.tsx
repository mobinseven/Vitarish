"use client";
import { use } from "react";
import { SurgeryPlanPage } from "@vitarish/clinic";
import { useClinicNavigation } from "@/components/clinic-navigation";
export default function SurgeryPlanEditRoute({
  params,
}: {
  params: Promise<{ surgeryId: string }>;
}) {
  const { surgeryId } = use(params);
  return (
    <SurgeryPlanPage
      mode="edit"
      surgeryId={surgeryId}
      onNavigate={useClinicNavigation()}
    />
  );
}

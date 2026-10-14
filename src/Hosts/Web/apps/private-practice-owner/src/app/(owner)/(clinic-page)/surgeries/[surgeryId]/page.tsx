"use client";
import { use } from "react";
import { SurgeryDetailPage } from "@vitarish/clinic";
import { useClinicNavigation } from "@/components/clinic-navigation";
export default function SurgeryDetailRoute({
  params,
}: {
  params: Promise<{ surgeryId: string }>;
}) {
  const { surgeryId } = use(params);
  return (
    <SurgeryDetailPage
      surgeryId={surgeryId}
      onNavigate={useClinicNavigation()}
    />
  );
}

"use client";
import { Suspense } from "react";
import { useSearchParams } from "next/navigation";
import { SurgeryPlanPage } from "@vitarish/clinic";
import { useClinicNavigation } from "@/components/clinic-navigation";
import { SurgeryPlanSearchParams } from "@/app/routes";

function SurgeryPlanCreateRouteContent() {
  const searchParams = useSearchParams();
  const patientId = searchParams.get(SurgeryPlanSearchParams.PatientId) ?? undefined;
  return (
    <SurgeryPlanPage
      mode="create"
      initialPatientId={patientId}
      onNavigate={useClinicNavigation()}
    />
  );
}

export default function SurgeryPlanCreateRoute() {
  return (
    <Suspense>
      <SurgeryPlanCreateRouteContent />
    </Suspense>
  );
}

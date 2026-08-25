"use client";
import { use } from "react";
import { PatientOverviewPage } from "@vitarish/clinic";
import { useClinicNavigation } from "@/components/clinic-navigation";
export default function PatientOverviewRoute({
  params,
}: {
  params: Promise<{ patientId: string }>;
}) {
  const { patientId } = use(params);
  return (
    <PatientOverviewPage
      patientId={patientId}
      onNavigate={useClinicNavigation()}
    />
  );
}

"use client";
import { use } from "react";
import { PatientEditPage } from "@vitarish/clinic-web";
import { useClinicNavigation } from "@/components/clinic-navigation";
export default function PatientEditRoute({
  params,
}: {
  params: Promise<{ patientId: string }>;
}) {
  const { patientId } = use(params);
  return (
    <PatientEditPage patientId={patientId} onNavigate={useClinicNavigation()} />
  );
}

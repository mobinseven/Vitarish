"use client";
import { use } from "react";
import { PatientProcedurePage } from "@vitarish/clinic";
import { useClinicNavigation } from "@/components/clinic-navigation";
export default function PatientProcedureRoute({
  params,
}: {
  params: Promise<{ patientId: string; procedureId: string }>;
}) {
  const { patientId, procedureId } = use(params);
  return (
    <PatientProcedurePage
      patientId={patientId}
      procedureId={procedureId}
      onNavigate={useClinicNavigation()}
    />
  );
}

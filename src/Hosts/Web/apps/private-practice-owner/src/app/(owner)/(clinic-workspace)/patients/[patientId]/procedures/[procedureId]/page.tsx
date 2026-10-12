"use client";
import { use } from "react";
import { useAuthSession } from "@vitarish/auth";
import { PatientProcedurePage } from "@vitarish/clinic";
import { useClinicNavigation } from "@/components/clinic-navigation";
export default function PatientProcedureRoute({
  params,
}: {
  params: Promise<{ patientId: string; procedureId: string }>;
}) {
  const { patientId, procedureId } = use(params);
  const { displayProfile, user } = useAuthSession();
  const viewer = user
    ? { id: user.userId, label: displayProfile.displayLabel }
    : undefined;
  return (
    <PatientProcedurePage
      patientId={patientId}
      procedureId={procedureId}
      viewer={viewer}
      onNavigate={useClinicNavigation()}
    />
  );
}

"use client";

import { useCallback } from "react";
import { useRouter } from "next/navigation";
import { Routes } from "@/app/routes";
import { PatientCheckInWizard } from "@/features/clinic/check-in/components/PatientCheckInWizard";

export default function ClinicPatientCheckInPage() {
  const router = useRouter();

  const handlePatientSelected = useCallback(
    (patientId: string) => {
      // Future: navigate to shift selection with patient context
      console.log("Patient selected for check-in:", patientId);
    },
    []
  );

  return (
    <PatientCheckInWizard onPatientSelected={handlePatientSelected} />
  );
}

"use client";

import { useCallback } from "react";
import { PatientCheckInWizard } from "@/features/clinic/check-in/components/PatientCheckInWizard";

export default function ClinicPatientCheckInPage() {
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

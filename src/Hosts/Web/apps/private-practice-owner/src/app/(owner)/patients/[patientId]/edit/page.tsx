"use client";
import { use } from "react";
import { ClinicPatientEditPage } from "@vitarish/clinic-web";
import { useClinicNavigation } from "@/components/clinic-navigation";
export default function ClinicPatientEditRoute({ params }: { params: Promise<{ patientId: string }> }) {
  const { patientId } = use(params);
  return <ClinicPatientEditPage patientId={patientId} onNavigate={useClinicNavigation()} />;
}

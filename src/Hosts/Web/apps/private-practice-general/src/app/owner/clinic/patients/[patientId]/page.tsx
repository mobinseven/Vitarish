"use client";
import { use } from "react";
import { ClinicPatientOverviewPage } from "@vitarish/clinic-web";
import { useClinicNavigation } from "@/components/clinic-navigation";
export default function ClinicPatientOverviewRoute({ params }: { params: Promise<{ patientId: string }> }) {
  const { patientId } = use(params);
  return <ClinicPatientOverviewPage patientId={patientId} onNavigate={useClinicNavigation()} />;
}

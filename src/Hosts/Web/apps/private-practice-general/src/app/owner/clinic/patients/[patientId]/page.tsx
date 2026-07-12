"use client";

import { use } from "react";
import { PatientOverviewPageSurface } from "@vitarish/clinic-web";
import { useClinicNavigation } from "@/components/clinic-navigation";

export default function PatientDetailRoute({
  params,
}: {
  params: Promise<{ patientId: string }>;
}) {
  const { patientId } = use(params);
  const navigate = useClinicNavigation();
  return <PatientOverviewPageSurface patientId={patientId} onNavigate={navigate} />;
}

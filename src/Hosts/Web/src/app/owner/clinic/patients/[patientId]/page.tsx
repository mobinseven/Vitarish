"use client";

import { use } from "react";
import { PatientOverviewPageSurface } from "@/features/clinic/patients/overview/components/PatientOverviewPageSurface";

export default function PatientDetailRoute({
  params,
}: {
  params: Promise<{ patientId: string }>;
}) {
  const { patientId } = use(params);
  return <PatientOverviewPageSurface patientId={patientId} />;
}

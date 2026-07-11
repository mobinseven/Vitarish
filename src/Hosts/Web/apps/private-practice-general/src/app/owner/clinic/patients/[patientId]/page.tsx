"use client";

import { use } from "react";
import { PatientOverviewPageSurface } from "@vitarish/clinic-web";

export default function PatientDetailRoute({
  params,
}: {
  params: Promise<{ patientId: string }>;
}) {
  const { patientId } = use(params);
  return <PatientOverviewPageSurface patientId={patientId} />;
}

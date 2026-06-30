"use client";

import { use } from "react";
import { ClinicOwnerPatientDetailPage } from "@/features/clinic/components";

export default function PatientDetailRoute({
  params,
}: {
  params: Promise<{ patientId: string }>;
}) {
  const { patientId } = use(params);
  return <ClinicOwnerPatientDetailPage patientId={patientId} />;
}

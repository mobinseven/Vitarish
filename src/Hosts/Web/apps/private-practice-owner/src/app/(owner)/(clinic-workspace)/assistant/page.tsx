"use client";

import { useRouter } from "next/navigation";
import { ClinicAssistantWorkspace } from "@vitarish/clinic";
import { Routes } from "@/app/routes";
import { useClinicNavigation } from "@/components/clinic-navigation";

export default function AssistantRoute() {
  const router = useRouter();
  return (
    <ClinicAssistantWorkspace
      onNavigate={useClinicNavigation()}
      onOpenPatient={(patientId) => router.push(Routes.Clinic.Patients.Detail(patientId))}
    />
  );
}

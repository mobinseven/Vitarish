"use client";
import { ClinicPatientCreatePage } from "@vitarish/clinic-web";
import { useClinicNavigation } from "@/components/clinic-navigation";
export default function ClinicPatientCreateRoute() {
  return <ClinicPatientCreatePage onNavigate={useClinicNavigation()} />;
}

"use client";
import { PatientCreatePage } from "@vitarish/clinic";
import { useClinicNavigation } from "@/components/clinic-navigation";
export default function PatientCreateRoute() {
  return <PatientCreatePage onNavigate={useClinicNavigation()} />;
}

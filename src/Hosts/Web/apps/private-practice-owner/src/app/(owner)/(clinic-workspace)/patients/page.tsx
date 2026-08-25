"use client";

import { PatientListPage } from "@vitarish/clinic";
import { useClinicNavigation } from "@/components/clinic-navigation";

export default function PatientListRoute() {
  return <PatientListPage onNavigate={useClinicNavigation()} />;
}

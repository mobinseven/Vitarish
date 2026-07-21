"use client";

import { PatientListPage } from "@vitarish/clinic-web";
import { useClinicNavigation } from "@/components/clinic-navigation";

export default function PatientListRoute() {
  return <PatientListPage onNavigate={useClinicNavigation()} />;
}

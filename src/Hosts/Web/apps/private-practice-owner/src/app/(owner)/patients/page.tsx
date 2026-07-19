"use client";

import { ClinicPatientListPage } from "@vitarish/clinic-web";
import { useClinicNavigation } from "@/components/clinic-navigation";

export default function ClinicPatientListRoute() {
  return <ClinicPatientListPage onNavigate={useClinicNavigation()} />;
}

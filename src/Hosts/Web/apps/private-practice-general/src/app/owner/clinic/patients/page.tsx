"use client";

import { ClinicOwnerPatientsPage } from "@vitarish/clinic-web";
import { useClinicNavigation } from "@/components/clinic-navigation";

export default function ClinicPatientsRoute() {
  const navigate = useClinicNavigation();
  return <ClinicOwnerPatientsPage onNavigate={navigate} />;
}

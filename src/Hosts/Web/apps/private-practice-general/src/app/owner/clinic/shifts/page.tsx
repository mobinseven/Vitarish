"use client";

import { ClinicOwnerShiftListPage } from "@vitarish/clinic-web";
import { useClinicNavigation } from "@/components/clinic-navigation";

export default function ClinicOwnerShiftListRoute() {
  return <ClinicOwnerShiftListPage onNavigate={useClinicNavigation()} />;
}

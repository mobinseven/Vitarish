"use client";

import { ClinicUserShiftListPage } from "@vitarish/clinic-web";
import { useClinicNavigation } from "@/components/clinic-navigation";

export default function ClinicUserShiftListRoute() {
  return <ClinicUserShiftListPage onNavigate={useClinicNavigation()} />;
}

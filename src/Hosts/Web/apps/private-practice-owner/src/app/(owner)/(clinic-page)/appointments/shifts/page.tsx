"use client";

import { OwnerShiftListPage } from "@vitarish/clinic";
import { useClinicNavigation } from "@/components/clinic-navigation";

export default function OwnerShiftListRoute() {
  return <OwnerShiftListPage onNavigate={useClinicNavigation()} />;
}

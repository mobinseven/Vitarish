"use client";

import { OwnerShiftListPage } from "@vitarish/clinic-web";
import { useClinicNavigation } from "@/components/clinic-navigation";

export default function OwnerShiftListRoute() {
  return <OwnerShiftListPage onNavigate={useClinicNavigation()} />;
}

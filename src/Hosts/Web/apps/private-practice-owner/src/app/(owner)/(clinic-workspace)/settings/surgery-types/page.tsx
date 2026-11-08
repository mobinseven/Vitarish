"use client";

import { SurgeryTypeManagementPanel } from "@vitarish/clinic";
import { useClinicNavigation } from "@/components/clinic-navigation";

export default function SurgeryTypeManagementRoute() {
  return <SurgeryTypeManagementPanel onNavigate={useClinicNavigation()} />;
}

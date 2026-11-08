"use client";

import { PrescriptionTemplateWorkspace } from "@vitarish/clinic";
import { useClinicNavigation } from "@/components/clinic-navigation";

export default function PrescriptionTemplateManagementRoute() {
  return <PrescriptionTemplateWorkspace onNavigate={useClinicNavigation()} />;
}

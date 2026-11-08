"use client";

import { ProcedureTemplateManagementPanel } from "@vitarish/clinic";
import { useClinicNavigation } from "@/components/clinic-navigation";

export default function ProcedureTemplateManagementRoute() {
  return <ProcedureTemplateManagementPanel onNavigate={useClinicNavigation()} />;
}

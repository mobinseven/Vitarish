"use client";

import { ClinicDashboardPage } from "@vitarish/clinic-web";
import { useClinicNavigation } from "@/components/clinic-navigation";

export default function ClinicDashboardRoute() {
  return <ClinicDashboardPage onNavigate={useClinicNavigation()} />;
}

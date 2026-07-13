"use client";

import { ClinicSettingsPage } from "@vitarish/clinic-web";
import { useClinicNavigation } from "@/components/clinic-navigation";

export default function ClinicSettingsRoute() {
  return <ClinicSettingsPage onNavigate={useClinicNavigation()} />;
}

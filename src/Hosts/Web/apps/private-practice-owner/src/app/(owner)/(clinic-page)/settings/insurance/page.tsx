"use client";

import { InsuranceProviderSettingsPage } from "@vitarish/clinic";
import { useClinicNavigation } from "@/components/clinic-navigation";

export default function InsuranceProviderSettingsRoute() {
  return <InsuranceProviderSettingsPage onNavigate={useClinicNavigation()} />;
}

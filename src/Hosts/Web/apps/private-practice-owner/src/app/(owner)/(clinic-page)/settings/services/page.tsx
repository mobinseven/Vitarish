"use client";

import { ServiceListPage } from "@vitarish/clinic";
import { useClinicNavigation } from "@/components/clinic-navigation";
import { ServicesTabs } from "@/components/settings/ServicesTabs";

export default function ServiceListRoute() {
  return <ServiceListPage onNavigate={useClinicNavigation()} tabs={<ServicesTabs />} />;
}

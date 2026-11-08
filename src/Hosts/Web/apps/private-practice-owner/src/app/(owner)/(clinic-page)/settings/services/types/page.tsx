"use client";

import { ServiceTypeListPage } from "@vitarish/clinic";
import { useClinicNavigation } from "@/components/clinic-navigation";
import { ServicesTabs } from "@/components/settings/ServicesTabs";

export default function ServiceTypeListRoute() {
  return <ServiceTypeListPage onNavigate={useClinicNavigation()} tabs={<ServicesTabs />} />;
}

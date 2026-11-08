"use client";

import { ServiceProviderListPage } from "@vitarish/clinic";
import { useClinicNavigation } from "@/components/clinic-navigation";
import { ServicesTabs } from "@/components/settings/ServicesTabs";

export default function ServiceProviderListRoute() {
  return <ServiceProviderListPage onNavigate={useClinicNavigation()} tabs={<ServicesTabs />} />;
}

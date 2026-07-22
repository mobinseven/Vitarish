"use client";
import { use } from "react";
import { ServiceProviderDetailPage } from "@vitarish/clinic-web";
import { useClinicNavigation } from "@/components/clinic-navigation";
export default function ServiceProviderDetailRoute({
  params,
}: {
  params: Promise<{ serviceProviderId: string }>;
}) {
  const { serviceProviderId } = use(params);
  return (
    <ServiceProviderDetailPage
      serviceProviderId={serviceProviderId}
      onNavigate={useClinicNavigation()}
    />
  );
}

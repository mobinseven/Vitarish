"use client";
import { use } from "react";
import { ServiceDetailPage } from "@vitarish/clinic-web";
import { useClinicNavigation } from "@/components/clinic-navigation";
export default function ServiceDetailRoute({
  params,
}: {
  params: Promise<{ serviceId: string }>;
}) {
  const { serviceId } = use(params);
  return (
    <ServiceDetailPage
      serviceId={serviceId}
      onNavigate={useClinicNavigation()}
    />
  );
}

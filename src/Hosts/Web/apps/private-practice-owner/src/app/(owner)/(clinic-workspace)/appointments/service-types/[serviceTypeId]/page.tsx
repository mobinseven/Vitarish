"use client";
import { use } from "react";
import { ServiceTypeDetailPage } from "@vitarish/clinic-web";
import { useClinicNavigation } from "@/components/clinic-navigation";
export default function ServiceTypeDetailRoute({
  params,
}: {
  params: Promise<{ serviceTypeId: string }>;
}) {
  const { serviceTypeId } = use(params);
  return (
    <ServiceTypeDetailPage
      serviceTypeId={serviceTypeId}
      onNavigate={useClinicNavigation()}
    />
  );
}

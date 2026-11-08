"use client";
import { use } from "react";
import { ServiceTypeDetailPage } from "@vitarish/clinic";
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

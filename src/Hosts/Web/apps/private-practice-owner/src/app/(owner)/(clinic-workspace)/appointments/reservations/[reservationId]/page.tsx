"use client";
import { use } from "react";
import { OwnerReservationDetailPage } from "@vitarish/clinic-web";
import { useClinicNavigation } from "@/components/clinic-navigation";
export default function OwnerReservationDetailRoute({
  params,
}: {
  params: Promise<{ reservationId: string }>;
}) {
  const { reservationId } = use(params);
  return (
    <OwnerReservationDetailPage
      reservationId={reservationId}
      onNavigate={useClinicNavigation()}
    />
  );
}

"use client";
import { use } from "react";
import { ClinicOwnerReservationDetailPage } from "@vitarish/clinic-web";
import { useClinicNavigation } from "@/components/clinic-navigation";
export default function ClinicOwnerReservationDetailRoute({ params }: { params: Promise<{ reservationId: string }> }) {
  const { reservationId } = use(params);
  return <ClinicOwnerReservationDetailPage reservationId={reservationId} onNavigate={useClinicNavigation()} />;
}

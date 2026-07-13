"use client";
import { use } from "react";
import { ClinicUserReservationDetailPage } from "@vitarish/clinic-web";
import { useClinicNavigation } from "@/components/clinic-navigation";
export default function ClinicUserReservationDetailRoute({ params }: { params: Promise<{ reservationId: string }> }) {
  const { reservationId } = use(params);
  return <ClinicUserReservationDetailPage reservationId={reservationId} onNavigate={useClinicNavigation()} />;
}

"use client";

import { UserReservationsList } from "@vitarish/clinic-web";
import { useClinicNavigation } from "@/components/clinic-navigation";

export default function ClinicUserReservationsPage() {
  const navigate = useClinicNavigation();
  return <UserReservationsList onNavigate={navigate} />;
}

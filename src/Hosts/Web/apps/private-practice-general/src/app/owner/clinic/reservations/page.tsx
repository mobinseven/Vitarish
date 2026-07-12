"use client";

import { OwnerReservationsAgenda } from "@vitarish/clinic-web";
import { useClinicNavigation } from "@/components/clinic-navigation";

export default function ClinicOwnerReservationsPage() {
  const navigate = useClinicNavigation();
  return <OwnerReservationsAgenda onNavigate={navigate} />;
}

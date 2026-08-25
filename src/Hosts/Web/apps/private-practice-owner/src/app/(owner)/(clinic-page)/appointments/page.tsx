"use client";

import { OwnerReservationListPage } from "@vitarish/clinic";
import { useClinicNavigation } from "@/components/clinic-navigation";

export default function AppointmentsRoute() {
  return <OwnerReservationListPage onNavigate={useClinicNavigation()} />;
}

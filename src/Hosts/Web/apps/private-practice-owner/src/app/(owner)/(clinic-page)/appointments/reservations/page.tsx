"use client";

import { OwnerReservationListPage } from "@vitarish/clinic-web";
import { useClinicNavigation } from "@/components/clinic-navigation";

export default function OwnerReservationListRoute() {
  return <OwnerReservationListPage onNavigate={useClinicNavigation()} />;
}

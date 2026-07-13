"use client";

import { ClinicOwnerReservationListPage } from "@vitarish/clinic-web";
import { useClinicNavigation } from "@/components/clinic-navigation";

export default function ClinicOwnerReservationListRoute() {
  return <ClinicOwnerReservationListPage onNavigate={useClinicNavigation()} />;
}

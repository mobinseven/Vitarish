"use client";

import { ClinicUserReservationListPage } from "@vitarish/clinic-web";
import { useClinicNavigation } from "@/components/clinic-navigation";

export default function ClinicUserReservationListRoute() {
  return <ClinicUserReservationListPage onNavigate={useClinicNavigation()} />;
}

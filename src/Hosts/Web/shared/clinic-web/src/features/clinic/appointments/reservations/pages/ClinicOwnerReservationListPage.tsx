"use client";

import type { ClinicNavigationIntent } from "../../../../../navigation";
import { OwnerReservationsAgenda } from "../../agenda/components/OwnerReservationsAgenda";

export interface ClinicOwnerReservationListPageProps {
  onNavigate: (intent: ClinicNavigationIntent) => void;
}

export function ClinicOwnerReservationListPage({ onNavigate }: ClinicOwnerReservationListPageProps) {
  return <OwnerReservationsAgenda onNavigate={onNavigate} />;
}

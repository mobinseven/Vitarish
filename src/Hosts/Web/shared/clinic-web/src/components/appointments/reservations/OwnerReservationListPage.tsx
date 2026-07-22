"use client";

import type { NavigationIntent } from "../../../navigation";
import { OwnerReservationsAgenda } from "../agenda/OwnerReservationsAgenda";

export interface OwnerReservationListPageProps {
  onNavigate: (intent: NavigationIntent) => void;
}

export function OwnerReservationListPage({
  onNavigate,
}: OwnerReservationListPageProps) {
  return <OwnerReservationsAgenda onNavigate={onNavigate} />;
}

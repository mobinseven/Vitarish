"use client";

import type { ClinicNavigationIntent } from "../../../navigation";
import { UserReservationsList } from "./UserReservationsList";

export interface ClinicUserReservationListPageProps {
  onNavigate: (intent: ClinicNavigationIntent) => void;
}

export function ClinicUserReservationListPage({ onNavigate }: ClinicUserReservationListPageProps) {
  return <UserReservationsList onNavigate={onNavigate} />;
}

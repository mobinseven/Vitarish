"use client";

import type { NavigationIntent } from "../../../navigation";
import { UserReservationsList } from "./UserReservationsList";

export interface UserReservationListPageProps {
  onNavigate: (intent: NavigationIntent) => void;
}

export function UserReservationListPage({ onNavigate }: UserReservationListPageProps) {
  return <UserReservationsList onNavigate={onNavigate} />;
}

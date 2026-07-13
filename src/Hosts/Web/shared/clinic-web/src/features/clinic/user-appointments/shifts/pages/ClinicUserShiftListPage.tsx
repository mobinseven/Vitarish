"use client";

import type { ClinicNavigationIntent } from "../../../../../navigation";
import { UserShiftsList } from "../components/UserShiftsList";

export interface ClinicUserShiftListPageProps {
  onNavigate: (intent: ClinicNavigationIntent) => void;
}

export function ClinicUserShiftListPage({ onNavigate }: ClinicUserShiftListPageProps) {
  return <UserShiftsList onNavigate={onNavigate} />;
}

"use client";

import type { ClinicNavigationIntent } from "../../../navigation";
import { OwnerShiftsList } from "./OwnerShiftsList";

export interface ClinicOwnerShiftListPageProps {
  onNavigate: (intent: ClinicNavigationIntent) => void;
}

export function ClinicOwnerShiftListPage({ onNavigate }: ClinicOwnerShiftListPageProps) {
  return <OwnerShiftsList onNavigate={onNavigate} />;
}

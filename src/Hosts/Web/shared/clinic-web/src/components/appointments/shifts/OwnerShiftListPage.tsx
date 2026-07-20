"use client";

import type { NavigationIntent } from "../../../navigation";
import { OwnerShiftsList } from "./OwnerShiftsList";

export interface OwnerShiftListPageProps {
  onNavigate: (intent: NavigationIntent) => void;
}

export function OwnerShiftListPage({ onNavigate }: OwnerShiftListPageProps) {
  return <OwnerShiftsList onNavigate={onNavigate} />;
}

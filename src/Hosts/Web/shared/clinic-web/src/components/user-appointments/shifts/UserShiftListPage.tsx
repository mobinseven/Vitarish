"use client";

import type { NavigationIntent } from "../../../navigation";
import { UserShiftsList } from "./UserShiftsList";

export interface UserShiftListPageProps {
  onNavigate: (intent: NavigationIntent) => void;
}

export function UserShiftListPage({ onNavigate }: UserShiftListPageProps) {
  return <UserShiftsList onNavigate={onNavigate} />;
}

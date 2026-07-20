"use client";

import type { NavigationIntent } from "../../navigation";
import { OwnerDashboard } from "./OwnerDashboard";

export interface DashboardPageProps {
  onNavigate: (intent: NavigationIntent) => void;
}

export function DashboardPage({ onNavigate }: DashboardPageProps) {
  return <OwnerDashboard onNavigate={onNavigate} />;
}

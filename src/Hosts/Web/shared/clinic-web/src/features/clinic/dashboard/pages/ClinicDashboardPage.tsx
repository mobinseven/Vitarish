"use client";

import type { ClinicNavigationIntent } from "../../../../navigation";
import { ClinicOwnerDashboard } from "../components/ClinicOwnerDashboard";

export interface ClinicDashboardPageProps {
  onNavigate: (intent: ClinicNavigationIntent) => void;
}

export function ClinicDashboardPage({ onNavigate }: ClinicDashboardPageProps) {
  return <ClinicOwnerDashboard onNavigate={onNavigate} />;
}

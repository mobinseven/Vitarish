"use client";

import { ClinicOwnerDashboard } from "@vitarish/clinic-web";
import { useClinicNavigation } from "@/components/clinic-navigation";

export default function ClinicOwnerDashboardPage() {
  const navigate = useClinicNavigation();
  return <ClinicOwnerDashboard onNavigate={navigate} />;
}

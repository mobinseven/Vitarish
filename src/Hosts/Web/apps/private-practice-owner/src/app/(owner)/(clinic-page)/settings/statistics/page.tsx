"use client";

import { StatisticsPage } from "@vitarish/clinic";
import { useClinicNavigation } from "@/components/clinic-navigation";

export default function StatisticsRoute() {
  return <StatisticsPage onNavigate={useClinicNavigation()} />;
}

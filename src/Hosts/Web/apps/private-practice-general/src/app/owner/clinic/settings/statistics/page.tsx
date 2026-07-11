import type { Metadata } from "next";
import { ClinicStatisticsDashboard } from "@vitarish/clinic-web";

export const metadata: Metadata = {
  title: "Clinic Statistics | Vitarish",
  description: "View clinic performance metrics and analytics",
};

export default function ClinicStatisticsPage() {
  return <ClinicStatisticsDashboard />;
}

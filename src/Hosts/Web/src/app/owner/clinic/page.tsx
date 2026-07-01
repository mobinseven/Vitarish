import type { Metadata } from "next";
import { ClinicOwnerDashboard } from "@/features/clinic/dashboard/components/ClinicOwnerDashboard";

export const metadata: Metadata = {
  title: "Clinic Dashboard | Vitarish",
  description: "Manage your clinic operations",
};

export default function ClinicOwnerDashboardPage() {
  return <ClinicOwnerDashboard />;
}

import type { Metadata } from "next";
import { ClinicOwnerDashboard } from "@vitarish/clinic-web";

export const metadata: Metadata = {
  title: "Clinic Dashboard | Vitarish",
  description: "Manage your clinic operations",
};

export default function ClinicOwnerDashboardPage() {
  return <ClinicOwnerDashboard />;
}

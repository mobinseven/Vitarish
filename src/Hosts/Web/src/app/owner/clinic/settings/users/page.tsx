import type { Metadata } from "next";
import { ClinicSettingsUsersSurface } from "@/features/clinic/settings/users/components/ClinicSettingsUsersSurface";

export const metadata: Metadata = {
  title: "Clinic Users | Vitarish",
  description: "Manage clinic staff accounts and permissions",
};

export default function ClinicSettingsUsersPage() {
  return <ClinicSettingsUsersSurface />;
}

import type { Metadata } from "next";
import { ClinicSettingsUsersSurface } from "@vitarish/clinic-web";

export const metadata: Metadata = {
  title: "Clinic Users | Vitarish",
  description: "Manage clinic staff accounts and permissions",
};

export default function ClinicSettingsUsersPage() {
  return <ClinicSettingsUsersSurface />;
}

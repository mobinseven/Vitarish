import type { Metadata } from "next";
import { ClinicSettingsDashboard } from "@/features/clinic/settings/components/ClinicSettingsDashboard";

export const metadata: Metadata = {
  title: "Clinic Settings | Vitarish",
  description: "Configure your clinic settings",
};

export default function ClinicSettingsPage() {
  return <ClinicSettingsDashboard />;
}

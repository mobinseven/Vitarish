import type { Metadata } from "next";
import { AppointmentSettingsDashboard } from "@vitarish/clinic-web";

export const metadata: Metadata = {
  title: "Appointment Settings | Vitarish",
  description: "Configure services, providers, and appointment policies",
};

export default function ClinicAppointmentSettingsPage() {
  return <AppointmentSettingsDashboard />;
}

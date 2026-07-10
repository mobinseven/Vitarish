import type { Metadata } from "next";
import { AppointmentSettingsDashboard } from "@/features/clinic/appointments/settings/components/AppointmentSettingsDashboard";

export const metadata: Metadata = {
  title: "Appointment Settings | Vitarish",
  description: "Configure services, providers, and appointment policies",
};

export default function ClinicAppointmentSettingsPage() {
  return <AppointmentSettingsDashboard />;
}

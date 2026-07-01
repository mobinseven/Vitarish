import type { Metadata } from "next";
import { ClinicOwnerRemindersGrid } from "@/features/clinic/reminders/components/ClinicOwnerRemindersGrid";

export const metadata: Metadata = {
  title: "Clinic Reminders | Vitarish",
  description: "View and manage patient reminders",
};

export default function ClinicOwnerRemindersPage() {
  return <ClinicOwnerRemindersGrid />;
}

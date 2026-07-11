import type { Metadata } from "next";
import { ClinicOwnerRemindersGrid } from "@vitarish/clinic-web";

export const metadata: Metadata = {
  title: "Clinic Reminders | Vitarish",
  description: "View and manage patient reminders",
};

export default function ClinicOwnerRemindersPage() {
  return <ClinicOwnerRemindersGrid />;
}

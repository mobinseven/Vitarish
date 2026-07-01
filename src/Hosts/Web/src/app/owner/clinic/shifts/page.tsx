import type { Metadata } from "next";
import { OwnerShiftsList } from "@/features/clinic/appointments/shifts/components/OwnerShiftsList";

export const metadata: Metadata = {
  title: "Shifts | Vitarish",
  description: "Manage clinic shifts and schedules",
};

export default function ClinicOwnerShiftsPage() {
  return <OwnerShiftsList />;
}

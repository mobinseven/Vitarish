import type { Metadata } from "next";
import { UserShiftsList } from "@/features/clinic/user-appointments/shifts/components/UserShiftsList";

export const metadata: Metadata = {
  title: "Available Shifts | Vitarish",
  description: "Browse and book available shifts",
};

export default function ClinicUserShiftsPage() {
  return <UserShiftsList />;
}

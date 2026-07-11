import type { Metadata } from "next";
import { OwnerShiftsList } from "@vitarish/clinic-web";

export const metadata: Metadata = {
  title: "Shifts | Vitarish",
  description: "Manage clinic shifts and schedules",
};

export default function ClinicOwnerShiftsPage() {
  return <OwnerShiftsList />;
}

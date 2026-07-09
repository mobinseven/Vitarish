import type { Metadata } from "next";
import { ClinicOwnerPatientsPage } from "@/features/clinic/components";

export const metadata: Metadata = {
  title: "Patients | Private Practice General | Vitarish",
  description: "Manage clinic patients.",
};

export default function ClinicPatientsRoute() {
  return <ClinicOwnerPatientsPage />;
}

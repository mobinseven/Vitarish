import { redirect } from "next/navigation";
import { Routes } from "@/app/routes";

export default function ClinicIndexRoute() {
  redirect(Routes.Clinic.Patients.Index);
}

import type { Metadata } from "next";
import { ServiceTypesPlaceholder } from "@/features/clinic/service-types/components/ServiceTypesPlaceholder";

export const metadata: Metadata = {
  title: "Service Types | Vitarish",
  description: "Manage service types offered by the clinic",
};

export default function ClinicServiceTypesPage() {
  return <ServiceTypesPlaceholder />;
}

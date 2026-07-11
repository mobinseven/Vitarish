import type { Metadata } from "next";
import { ServiceTypesList } from "@vitarish/clinic-web";

export const metadata: Metadata = {
  title: "Service Types | Vitarish",
  description: "Manage service types offered by the clinic",
};

export default function ClinicServiceTypesPage() {
  return <ServiceTypesList />;
}

import type { Metadata } from "next";
import { ServicesPlaceholder } from "@vitarish/clinic-web";

export const metadata: Metadata = {
  title: "Services | Vitarish",
  description: "Manage individual services and their configurations",
};

export default function ClinicServicesPage() {
  return <ServicesPlaceholder />;
}

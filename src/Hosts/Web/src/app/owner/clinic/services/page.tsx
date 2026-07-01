import type { Metadata } from "next";
import { ServicesPlaceholder } from "@/features/clinic/services/components/ServicesPlaceholder";

export const metadata: Metadata = {
  title: "Services | Vitarish",
  description: "Manage individual services and their configurations",
};

export default function ClinicServicesPage() {
  return <ServicesPlaceholder />;
}

import type { Metadata } from "next";
import { ServiceProvidersPlaceholder } from "@/features/clinic/service-providers/components/ServiceProvidersPlaceholder";

export const metadata: Metadata = {
  title: "Service Providers | Vitarish",
  description: "Manage healthcare providers and their specialties",
};

export default function ClinicServiceProvidersPage() {
  return <ServiceProvidersPlaceholder />;
}

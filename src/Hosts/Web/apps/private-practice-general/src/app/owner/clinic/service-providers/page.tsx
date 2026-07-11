import type { Metadata } from "next";
import { ServiceProvidersPlaceholder } from "@vitarish/clinic-web";

export const metadata: Metadata = {
  title: "Service Providers | Vitarish",
  description: "Manage healthcare providers and their specialties",
};

export default function ClinicServiceProvidersPage() {
  return <ServiceProvidersPlaceholder />;
}

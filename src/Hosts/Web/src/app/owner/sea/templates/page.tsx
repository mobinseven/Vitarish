import type { Metadata } from "next";
import { SeaTemplateProjectsPage } from "@/features/sea/templates/components";

export const metadata: Metadata = {
  title: "SEA Templates | Vitarish",
  description: "Browse SEA template projects.",
};

export default function SeaTemplatesRoute() {
  return <SeaTemplateProjectsPage />;
}

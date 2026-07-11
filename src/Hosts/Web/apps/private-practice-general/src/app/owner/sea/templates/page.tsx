import type { Metadata } from "next";
import { SeaTemplateProjectsPage } from "@vitarish/sea-web";

export const metadata: Metadata = {
  title: "SEA Templates | Vitarish",
  description: "Browse SEA template projects.",
};

export default function SeaTemplatesRoute() {
  return <SeaTemplateProjectsPage />;
}

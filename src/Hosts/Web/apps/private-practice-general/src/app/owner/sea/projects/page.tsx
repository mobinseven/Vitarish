import type { Metadata } from "next";
import { SeaProjectsPage } from "@/features/sea/projects/components";

export const metadata: Metadata = {
  title: "SEA Projects | Vitarish",
  description: "Manage SEA projects and procedures.",
};

export default function SeaProjectsListRoute() {
  return <SeaProjectsPage />;
}

import type { Metadata } from "next";
import { SeaProjectsPage } from "@vitarish/sea-web";

export const metadata: Metadata = {
  title: "SEA Projects | Vitarish",
  description: "Manage SEA projects and procedures.",
};

export default function SeaProjectsRoute() {
  return <SeaProjectsPage />;
}

import type { Metadata } from "next";
import { SeaDashboardPage } from "@vitarish/sea-web";

export const metadata: Metadata = {
  title: "SEA Statistics | Vitarish",
  description: "Review SEA project statistics.",
};

export default function SeaStatisticsRoute() {
  return <SeaDashboardPage />;
}

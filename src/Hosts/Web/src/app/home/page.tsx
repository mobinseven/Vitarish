import type { Metadata } from "next";
import { HomeDashboard } from "@/features/home/components/HomeDashboard";

export const metadata: Metadata = {
  title: "Home | Vitarish",
  description: "Choose the next Vitarish module demo to explore.",
};

export default function HomePage() {
  return <HomeDashboard />;
}

import type { Metadata } from "next";
import { DashboardShell } from "@/components/shell";
import {
  dashboardFooterItems,
  homeDashboardNavGroups,
} from "@/components/shell/dashboardShellNavigation";
import { HomeDashboard } from "@/features/home/components/HomeDashboard";
import { ContainedPage } from "@vitarish/ui/layout";

export const metadata: Metadata = {
  title: "Home | Vitarish",
  description: "Choose the next Vitarish module demo to explore.",
};

export default function HomePage() {
  return (
    <DashboardShell
      footerItems={dashboardFooterItems}
      navGroups={homeDashboardNavGroups}
    >
      <ContainedPage>
        <HomeDashboard />
      </ContainedPage>
    </DashboardShell>
  );
}

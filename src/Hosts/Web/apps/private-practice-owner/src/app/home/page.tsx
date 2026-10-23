import type { Metadata } from "next";
import { JourneyGateBoundary } from "@/components/journey/JourneyGateBoundary";
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
      <ContainedPage width="fluid">
        {/* On a platform host, such as the sign-in or onboarding host, the journey sends a signed-in visitor on from here. */}
        <JourneyGateBoundary>
          <HomeDashboard />
        </JourneyGateBoundary>
      </ContainedPage>
    </DashboardShell>
  );
}

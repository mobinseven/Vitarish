import { JourneyGateBoundary } from "@/components/journey/JourneyGateBoundary";
import { DashboardShell } from "@/components/shell";
import {
  clinicOwnerDashboardNavGroups,
  dashboardFooterItems,
} from "@/components/shell/dashboardShellNavigation";
import { ContainedPage } from "@vitarish/ui/layout";

export default function ClinicPageLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <DashboardShell
      clinicOperationsAssistant
      footerItems={dashboardFooterItems}
      navGroups={clinicOwnerDashboardNavGroups}
    >
      <ContainedPage width="content">
        <JourneyGateBoundary>{children}</JourneyGateBoundary>
      </ContainedPage>
    </DashboardShell>
  );
}

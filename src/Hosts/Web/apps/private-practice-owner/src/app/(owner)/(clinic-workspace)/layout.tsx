import { JourneyGateBoundary } from "@/components/journey/JourneyGateBoundary";
import { DashboardShell } from "@/components/shell";
import {
  clinicOwnerDashboardNavGroups,
  dashboardFooterItems,
} from "@/components/shell/dashboardShellNavigation";
import { ImmersiveRegion, PageContainer } from "@vitarish/web-ui/layout";

export default function AssistantLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <DashboardShell
      clinicOperationsAssistant
      contentMode="workspace"
      footerItems={dashboardFooterItems}
      navGroups={clinicOwnerDashboardNavGroups}
    >
      <ImmersiveRegion>
        <PageContainer
          className="flex min-h-0 flex-1 flex-col py-6 lg:py-8"
          width="content"
        >
          <JourneyGateBoundary>{children}</JourneyGateBoundary>
        </PageContainer>
      </ImmersiveRegion>
    </DashboardShell>
  );
}

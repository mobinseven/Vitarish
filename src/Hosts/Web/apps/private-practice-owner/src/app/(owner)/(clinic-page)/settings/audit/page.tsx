import { SecurityAuditReviewPage } from "@vitarish/security";
import { SettingsChildPage } from "@/components/settings/SettingsChildPage";

export default function SecurityAuditRoute() {
  return (
    <SettingsChildPage>
      <SecurityAuditReviewPage />
    </SettingsChildPage>
  );
}

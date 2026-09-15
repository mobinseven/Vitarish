"use client";

import { useAuthSession, usePlatformAuthorization } from "@vitarish/auth";
import { usePartiesPlatformGetTenants } from "@vitarish/platform";
import { PlatformSecurityAuditReviewPage } from "@vitarish/security";
import { CenteredRegion } from "@vitarish/ui/layout";
import { useLocalizedText } from "@vitarish/localization";

export default function AuditPage() {
  const { isAuthenticated, isLoading: sessionLoading } = useAuthSession();
  const { hasAccess } = usePlatformAuthorization();
  const { t } = useLocalizedText();
  const tenants = usePartiesPlatformGetTenants({ query: { enabled: isAuthenticated && hasAccess } });

  if (sessionLoading) return <CenteredRegion><p>{t("common.states.loading")}</p></CenteredRegion>;
  if (!isAuthenticated || !hasAccess) return <CenteredRegion><h1 className="text-2xl font-semibold text-danger">{t("admin.forbiddenTitle")}</h1><p className="mt-2 text-muted">{t("admin.forbiddenDescription")}</p></CenteredRegion>;

  return (
    <PlatformSecurityAuditReviewPage
      tenants={tenants.data ?? []}
      tenantsLoading={tenants.isLoading}
      tenantsError={tenants.isError}
      onRetryTenants={() => void tenants.refetch()}
    />
  );
}

"use client";

import { useAuthSession, usePlatformAuthorization } from "@vitarish/auth";
import { CenteredRegion } from "@vitarish/ui/layout";
import { useLocalizedText } from "@vitarish/localization";
import Link from "next/link";
import { Routes } from "./routes";

export default function RootPage() {
  const { isAuthenticated, isLoading } = useAuthSession();
  const { hasAccess } = usePlatformAuthorization();
  const { t } = useLocalizedText();

  if (isLoading) return <CenteredRegion><p>{t("common.states.loading")}</p></CenteredRegion>;
  if (!isAuthenticated) return <CenteredRegion><h1 className="text-2xl font-semibold">{t("admin.title")}</h1><p className="mt-2 text-muted">{t("admin.signInRequired")}</p></CenteredRegion>;
  if (!hasAccess) return <CenteredRegion><h1 className="text-2xl font-semibold text-danger">{t("admin.forbiddenTitle")}</h1><p className="mt-2 text-muted">{t("admin.forbiddenDescription")}</p></CenteredRegion>;
  return <CenteredRegion><h1 className="text-2xl font-semibold">{t("admin.foundationTitle")}</h1><p className="mt-2 max-w-xl text-muted">{t("admin.foundationDescription")}</p><nav className="mt-6 flex flex-wrap gap-4"><Link className="text-primary underline" href={Routes.Users.Index}>{t("admin.usersLink")}</Link><Link className="text-primary underline" href={Routes.Owners.Index}>{t("admin.ownersLink")}</Link><Link className="text-primary underline" href={Routes.Tenants.Index}>{t("admin.tenantsLink")}</Link><Link className="text-primary underline" href={Routes.Settings.Index}>{t("admin.settingsLink")}</Link></nav></CenteredRegion>;
}

"use client";

import { ClinicEmptyState } from "@/features/clinic/components/ClinicStateSurface";
import { useLocalizedText } from "@/localization";

export function ClinicSettingsUsersSurface() {
  const { t } = useLocalizedText();
  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold">{t("clinic.settings.users.title")}</h1>
        <p className="text-sm text-muted">{t("clinic.settings.users.description")}</p>
      </div>

      <ClinicEmptyState title={t("clinic.settings.users.comingSoon")} />
    </div>
  );
}

"use client";

import { EmptyStateSurface as ClinicEmptyState } from "@vitarish/web-ui";
import { useLocalizedText } from "../../../localization/index";

export function SettingsUsersSurface() {
  const { t } = useLocalizedText();
  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold">
          {t("clinic.settings.users.title")}
        </h1>
        <p className="text-sm text-muted">
          {t("clinic.settings.users.description")}
        </p>
      </div>

      <ClinicEmptyState title={t("clinic.settings.users.comingSoon")} />
    </div>
  );
}

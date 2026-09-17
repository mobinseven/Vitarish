"use client";

import { UserManagementSurface } from "@vitarish/users";
import { useLocalizedText } from "../../../localization/index";

export function SettingsUsersSurface() {
  const { t } = useLocalizedText();
  return (
    <UserManagementSurface
      mode="tenant"
      title={t("clinic.settings.users.title")}
      description={t("clinic.settings.users.description")}
    />
  );
}

"use client";

import { Card } from "@heroui/react";
import { useLocalizedText } from "@/localization";

export function ClinicSettingsUsersSurface() {
  const { t } = useLocalizedText();
  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold">{t("clinic.settings.users.title")}</h1>
        <p className="text-sm text-muted">{t("clinic.settings.users.description")}</p>
      </div>

      <Card>
        <Card.Content className="flex flex-col items-center justify-center py-12">
          <p className="text-sm text-muted">{t("clinic.settings.users.comingSoon")}</p>
        </Card.Content>
      </Card>
    </div>
  );
}

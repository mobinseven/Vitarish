"use client";

import { Card } from "@heroui/react";
import { useLocalizedText } from "@/localization";

export function ServicesPlaceholder() {
  const { t } = useLocalizedText();
  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold">{t("clinic.services.title")}</h1>
        <p className="text-sm text-default-500">{t("clinic.services.description")}</p>
      </div>

      <Card>
        <Card.Content className="flex flex-col items-center justify-center py-12">
          <p className="text-sm text-default-500">
            {t("clinic.services.comingSoon")}
          </p>
        </Card.Content>
      </Card>
    </div>
  );
}

"use client";

import { Card } from "@heroui/react";
import { useLocalizedText } from "@/localization";

export function ServiceTypesPlaceholder() {
  const { t } = useLocalizedText();
  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold">{t("clinic.serviceTypes.title")}</h1>
        <p className="text-sm text-default-500">{t("clinic.serviceTypes.description")}</p>
      </div>

      <Card>
        <Card.Content className="flex flex-col items-center justify-center py-12">
          <p className="text-sm text-default-500">
            {t("clinic.serviceTypes.comingSoon")}
          </p>
        </Card.Content>
      </Card>
    </div>
  );
}

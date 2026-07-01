"use client";

import { Alert, Button } from "@heroui/react";
import { useLocalizedText } from "@/localization";

export function ContactConfirmationPanel() {
  const { t } = useLocalizedText();

  return (
    <div className="flex flex-col gap-5">
      <Alert status="warning">
        <Alert.Indicator />
        <Alert.Content>
          {t("journey.gates.contact.unavailable")}
        </Alert.Content>
      </Alert>
      <div className="border border-default-200 rounded-lg p-3 flex flex-col gap-4">
        <h2 className="text-base font-semibold text-foreground">{t("journey.gates.contact.optionsTitle")}</h2>
        <p className="text-sm leading-6 text-foreground/70">
          {t("journey.gates.contact.optionsDescription")}
        </p>
        <hr className="border-default-200" />
        <div className="grid gap-4 sm:grid-cols-2">
          <Button variant="secondary" isDisabled>{t("journey.gates.contact.emailUnavailable")}</Button>
          <Button variant="secondary" isDisabled>{t("journey.gates.contact.phoneUnavailable")}</Button>
        </div>
      </div>
    </div>
  );
}

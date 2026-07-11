"use client";

import { useState } from "react";
import { Alert, Button, Card, Checkbox } from "@heroui/react";
import { useLocalizedText } from "@vitarish/web-localization";

export function ConsentReviewPanel() {
  const [acceptedRequiredConsents, setAcceptedRequiredConsents] = useState(false);
  const { t } = useLocalizedText();

  return (
    <div className="flex flex-col gap-5">
      <Card>
        <Card.Content className="flex flex-col gap-4 p-4">
          <h2 className="text-base font-semibold text-foreground">{t("journey.gates.consent.panelTitle")}</h2>
          <p className="text-sm leading-6 text-foreground/70">
            {t("journey.gates.consent.panelDescription")}
          </p>
          <div className="h-px bg-separator" />
          <Checkbox
            isSelected={acceptedRequiredConsents}
            onChange={setAcceptedRequiredConsents}
          >
            <Checkbox.Content>
              <Checkbox.Control>
                <Checkbox.Indicator />
              </Checkbox.Control>
              {t("journey.gates.consent.acceptedLabel")}
            </Checkbox.Content>
          </Checkbox>
        </Card.Content>
      </Card>
      <Alert status="warning">
        <Alert.Indicator />
        <Alert.Content>{t("journey.gates.consent.pendingApi")}</Alert.Content>
      </Alert>
      <Button isDisabled={!acceptedRequiredConsents} variant="primary" className="w-full">
        {t("journey.gates.consent.continueWhenConnected")}
      </Button>
    </div>
  );
}

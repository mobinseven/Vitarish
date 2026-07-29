"use client";

import { Button, Tooltip } from "@heroui/react";
import { FaIcon } from "@vitarish/web-ui";
import { useLocalizedText } from "../../localization";

export function PatientAgentActivity() {
  const { t } = useLocalizedText();
  return (
    <p role="status" className="text-sm text-muted">
      <FaIcon icon="spinner" className="me-2" aria-hidden="true" />
      {t("clinic.patientAgent.running")}
    </p>
  );
}

export function PatientAgentErrorState({ onRetry }: { onRetry?: () => void }) {
  const { t } = useLocalizedText();
  return (
    <div role="alert" className="rounded-lg border border-danger p-3">
      <p>{t("clinic.patientAgent.error")}</p>
      {onRetry ? (
        <Tooltip>
          <Button variant="outline" onPress={onRetry}>
            <FaIcon icon="rotate-right" aria-hidden="true" />
            {t("clinic.patientAgent.retry")}
          </Button>
          <Tooltip.Content>{t("clinic.patientAgent.retry")}</Tooltip.Content>
        </Tooltip>
      ) : null}
    </div>
  );
}

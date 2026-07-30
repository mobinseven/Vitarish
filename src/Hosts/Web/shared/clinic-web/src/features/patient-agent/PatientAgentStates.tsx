"use client";

import { ChatMessage, ChatMessageActions } from "@heroui-pro/react";
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
    <ChatMessage.Assistant data-kind="Failure">
      <ChatMessage.Avatar
        alt={t("clinic.patientAgent.title")}
        fallback="AI"
        show
      />
      <ChatMessage.Body>
        <ChatMessage.Content>
          <p role="alert" className="text-danger">
            <FaIcon icon="circle-exclamation" className="me-2" aria-hidden="true" />
            {t("clinic.patientAgent.error")}
          </p>
        </ChatMessage.Content>
        {onRetry ? (
          <ChatMessageActions>
            <ChatMessageActions.Regenerate
              aria-label={t("clinic.patientAgent.retry")}
              tooltip={t("clinic.patientAgent.retry")}
              onPress={onRetry}
            />
          </ChatMessageActions>
        ) : null}
      </ChatMessage.Body>
    </ChatMessage.Assistant>
  );
}

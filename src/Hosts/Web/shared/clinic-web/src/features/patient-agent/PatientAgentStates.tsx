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

export function PatientAgentErrorState({ onRetry, scope = "patient" }: { onRetry?: () => void; scope?: "patient" | "operations" }) {
  const { t } = useLocalizedText();
  const title = scope === "operations" ? t("clinic.operationsAgent.title") : t("clinic.patientAgent.title");
  const error = scope === "operations" ? t("clinic.operationsAgent.error") : t("clinic.patientAgent.error");
  const retry = scope === "operations" ? t("clinic.operationsAgent.retry") : t("clinic.patientAgent.retry");
  return (
    <ChatMessage.Assistant data-kind="Failure">
      <ChatMessage.Avatar
        alt={title}
        fallback="AI"
        show
      />
      <ChatMessage.Body>
        <ChatMessage.Content>
          <p role="alert" className="text-danger">
            <FaIcon icon="circle-exclamation" className="me-2" aria-hidden="true" />
            {error}
          </p>
        </ChatMessage.Content>
        {onRetry ? (
          <ChatMessageActions>
            <ChatMessageActions.Regenerate
              aria-label={retry}
              tooltip={retry}
              onPress={onRetry}
            />
          </ChatMessageActions>
        ) : null}
      </ChatMessage.Body>
    </ChatMessage.Assistant>
  );
}

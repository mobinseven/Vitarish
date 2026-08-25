"use client";

import { ChatMessage, ChatMessageActions } from "@heroui-pro/react";
import { FaIcon } from "@vitarish/ui";
import { useLocalizedText } from "../../localization";

export function AssistantActivity() {
  const { t } = useLocalizedText();
  return (
    <p role="status" className="text-sm text-muted">
      <FaIcon icon="spinner" className="me-2" aria-hidden="true" />
      {t("clinic.clinicAssistant.running")}
    </p>
  );
}

export function AssistantErrorState({ onRetry, scope = "patient" }: { onRetry?: () => void; scope?: "patient" | "operations" }) {
  const { t } = useLocalizedText();
  const title = scope === "operations" ? t("clinic.operationsAssistant.title") : t("clinic.clinicAssistant.title");
  const error = scope === "operations" ? t("clinic.operationsAssistant.error") : t("clinic.clinicAssistant.error");
  const retry = scope === "operations" ? t("clinic.operationsAssistant.retry") : t("clinic.clinicAssistant.retry");
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

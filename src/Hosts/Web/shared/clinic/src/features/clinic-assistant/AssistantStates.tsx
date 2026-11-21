"use client";

import { ChatMessage, ChatMessageActions } from "@heroui-pro/react";
import type { ActiveTurnProvisionalAnswer } from "@vitarish/intelligence";
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

/**
 * The answer a streamed operations turn is writing, as plain text. It is provisional: the reply that follows replaces
 * it, and nothing actionable renders from it. Repaints are batched upstream and the text is not announced until
 * the final reply, so a screen reader is not interrupted mid-sentence.
 */
export function AssistantProvisionalReply({ answer }: { answer: ActiveTurnProvisionalAnswer }) {
  const { t } = useLocalizedText();
  return (
    <ChatMessage.Assistant data-kind="Provisional">
      <ChatMessage.Avatar alt={t("clinic.operationsAssistant.title")} fallback="AI" show />
      <ChatMessage.Body>
        <ChatMessage.Content>
          <p key={`${answer.turnId}:${answer.messageId}`} dir="auto" aria-live="off" aria-busy="true" className="whitespace-pre-wrap">{answer.text}</p>
        </ChatMessage.Content>
      </ChatMessage.Body>
    </ChatMessage.Assistant>
  );
}

/** A turn that was accepted but whose outcome never arrived: no retry is offered, because the answer may be saved. */
export function AssistantOutcomeUnknownState() {
  const { t } = useLocalizedText();
  return (
    <ChatMessage.Assistant data-kind="OutcomeUnknown">
      <ChatMessage.Avatar alt={t("clinic.operationsAssistant.title")} fallback="AI" show />
      <ChatMessage.Body>
        <ChatMessage.Content>
          <p role="alert" className="text-foreground">
            <FaIcon icon="circle-exclamation" className="me-2" aria-hidden="true" />
            {t("clinic.operationsAssistant.outcomeUnknown")}
          </p>
        </ChatMessage.Content>
      </ChatMessage.Body>
    </ChatMessage.Assistant>
  );
}

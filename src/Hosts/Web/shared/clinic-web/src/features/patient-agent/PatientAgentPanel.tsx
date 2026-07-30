"use client";

import {
  ChatConversation,
  ChatLoader,
  ChatMessage,
  PromptInput,
  Sheet,
} from "@heroui-pro/react";
import { FaIcon } from "@vitarish/web-ui";
import { type ReactNode, useEffect, useRef, useState } from "react";
import { useLocalizedText } from "../../localization";
import type { PatientAgentMessageResult } from "./hooks";
import { usePatientAgentConversation } from "./hooks";
import { PatientAgentActionList } from "./PatientAgentActions";
import {
  PatientAgentActivity,
  PatientAgentErrorState,
} from "./PatientAgentStates";

export {
  PatientAgentActivity,
  PatientAgentErrorState,
} from "./PatientAgentStates";

export function PatientAgentHeader({ patientName }: { patientName: string }) {
  const { t } = useLocalizedText();
  return (
    <>
      <Sheet.Heading>{t("clinic.patientAgent.title")}</Sheet.Heading>
      <p className="text-sm text-muted">
        {t("clinic.patientAgent.patient", { patient: patientName })}
      </p>
    </>
  );
}

export function PatientAgentComposer({
  disabled,
  onSubmit,
}: {
  disabled: boolean;
  onSubmit: (message: string) => Promise<void>;
}) {
  const { t } = useLocalizedText();
  const [message, setMessage] = useState("");
  const submit = async () => {
    const value = message.trim();
    if (!value || disabled) return;
    setMessage("");
    await onSubmit(value);
  };

  return (
    <PromptInput
      isDisabled={disabled}
      status={disabled ? "submitted" : "ready"}
      value={message}
      onSubmit={() => void submit()}
      onValueChange={setMessage}
      className="w-full"
      variant="secondary"
    >
      <PromptInput.Shell>
        <PromptInput.Content>
          <PromptInput.TextArea
            aria-label={t("clinic.patientAgent.composer.label")}
            placeholder={t("clinic.patientAgent.composer.placeholder")}
          />
        </PromptInput.Content>
        <PromptInput.Toolbar>
          <PromptInput.ToolbarEnd>
            <PromptInput.Send
              aria-label={t("clinic.patientAgent.composer.send")}
            />
          </PromptInput.ToolbarEnd>
        </PromptInput.Toolbar>
      </PromptInput.Shell>
    </PromptInput>
  );
}

type PatientAgentChatMessage =
  PatientAgentMessageResult | { kind: "UserMessage"; content: string };

export function PatientAgentMessage({
  message,
  onRetry,
}: {
  message: PatientAgentChatMessage;
  onRetry?: () => void;
}) {
  const { t } = useLocalizedText();
  if (message.kind === "UserMessage")
    return (
      <ChatMessage.User data-kind={message.kind}>
        <ChatMessage.Bubble>
          <ChatMessage.Content className="whitespace-pre-wrap">
            {message.content}
          </ChatMessage.Content>
        </ChatMessage.Bubble>
      </ChatMessage.User>
    );

  if (message.kind === "Failure") {
    return <PatientAgentErrorState onRetry={onRetry} />;
  }

  const labels = {
    AssistantMessage: "assistant",
    InformationalToolResult: "information",
    MissingInformationRequest: "missing",
    Failure: "failure",
  } as const;
  return (
    <ChatMessage.Assistant data-kind={message.kind}>
      <ChatMessage.Avatar
        alt={t("clinic.patientAgent.title")}
        fallback="AI"
        show
      />
      <ChatMessage.Body>
        <ChatMessage.Content>
          <p className="mb-1 text-xs font-semibold text-muted">
            {t(`clinic.patientAgent.messages.${labels[message.kind]}`)}
          </p>
          <p className="whitespace-pre-wrap">{message.content}</p>
        </ChatMessage.Content>
      </ChatMessage.Body>
    </ChatMessage.Assistant>
  );
}

export function PatientAgentMessageList({
  messages,
  onRetry,
  children,
}: {
  messages: PatientAgentChatMessage[];
  onRetry?: () => void;
  children?: ReactNode;
}) {
  const { t } = useLocalizedText();
  return (
    <ChatConversation
      className="min-h-0 flex-1"
      aria-label={t("clinic.patientAgent.messages.label")}
    >
      <ChatConversation.Content className="mx-auto flex w-full max-w-3xl flex-col gap-6 px-4 py-5">
        {messages.map((message, index) => (
          <PatientAgentMessage
            key={`${message.kind}-${index}`}
            message={message}
            onRetry={message.kind === "Failure" ? onRetry : undefined}
          />
        ))}
        {children}
        <ChatConversation.ScrollAnchor />
      </ChatConversation.Content>
      <ChatConversation.ScrollButton
        aria-label={t("clinic.patientAgent.messages.label")}
        tooltip={t("clinic.patientAgent.messages.label")}
      />
    </ChatConversation>
  );
}

export function PatientAgentPanel({
  patientId,
  patientName,
  isOpen,
  onOpenChange,
}: {
  patientId: string;
  patientName: string;
  isOpen: boolean;
  onOpenChange: (isOpen: boolean) => void;
}) {
  const { t, locale } = useLocalizedText();
  const conversation = usePatientAgentConversation(patientId);
  const [conversationId, setConversationId] = useState<string | null>(null);
  const [previousResponseId, setPreviousResponseId] = useState<string | null>(null);
  const [messages, setMessages] = useState<PatientAgentChatMessage[]>([]);
  const [actionIds, setActionIds] = useState<string[]>([]);
  const [lastMessage, setLastMessage] = useState("");
  const focusTarget = useRef<HTMLDivElement>(null);
  const activePatient = useRef(patientId);

  useEffect(() => {
    activePatient.current = patientId;
    setConversationId(null);
    setPreviousResponseId(null);
    setMessages([]);
    setActionIds([]);
    conversation.reset();
    // The mutation result is deliberately discarded whenever the trusted route patient changes.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [patientId]);

  useEffect(() => {
    if (messages.length || actionIds.length) focusTarget.current?.focus();
  }, [messages.length, actionIds.length]);

  const submit = async (message: string, showUserMessage = true) => {
    const submittedPatient = patientId;
    setLastMessage(message);
    if (showUserMessage)
      setMessages((current) => [
        ...current,
        { kind: "UserMessage", content: message },
      ]);
    const result = await conversation.submit(message, conversationId, previousResponseId);
    if (activePatient.current !== submittedPatient) return;
    const hasFailure = result.messages.some(({ kind }) => kind === "Failure");
    const responseMessages = hasFailure
      ? result.messages.filter(({ kind }) => kind === "Failure")
      : result.messages;
    setConversationId(result.conversationId);
    setPreviousResponseId(
      hasFailure ? previousResponseId : result.previousResponseId ?? null,
    );
    setMessages((current) => [...current, ...responseMessages]);
    setActionIds((current) => [
      ...current,
      ...(hasFailure ? [] : result.actions)
        .map((action) => action.id)
        .filter((id) => !current.includes(id)),
    ]);
  };

  return (
    <Sheet isOpen={isOpen} placement="bottom" onOpenChange={onOpenChange}>
      <Sheet.Backdrop>
        <Sheet.Content className="mx-auto max-h-[96vh] max-w-3xl">
          <Sheet.Dialog className="flex h-[88vh] flex-col">
            <Sheet.Handle />
            <Sheet.Header>
              <PatientAgentHeader patientName={patientName} />
              <Sheet.CloseTrigger aria-label={t("common.actions.close")}>
                <FaIcon icon="xmark" aria-hidden="true" />
              </Sheet.CloseTrigger>
            </Sheet.Header>
            <Sheet.Body className="flex min-h-0 flex-1 overflow-hidden p-0">
              <section
                dir={locale === "fa" ? "rtl" : "ltr"}
                className="flex min-h-0 flex-1 flex-col"
                aria-label={t("clinic.patientAgent.title")}
              >
                <div
                  ref={focusTarget}
                  tabIndex={-1}
                  className="flex min-h-0 flex-1 flex-col outline-none"
                >
                  <PatientAgentMessageList
                    messages={messages}
                    onRetry={
                      lastMessage
                        ? () => void submit(lastMessage, false)
                        : undefined
                    }
                  >
                    {conversation.isPending ? (
                      <ChatMessage.Assistant>
                        <ChatMessage.Avatar
                          alt={t("clinic.patientAgent.title")}
                          fallback="AI"
                          show
                        />
                        <ChatMessage.Body>
                          <ChatLoader.Dots
                            label={t("clinic.patientAgent.running")}
                          />
                        </ChatMessage.Body>
                      </ChatMessage.Assistant>
                    ) : null}
                    {conversation.isError ? (
                      <PatientAgentErrorState
                        onRetry={
                          lastMessage
                            ? () => void submit(lastMessage, false)
                            : undefined
                        }
                      />
                    ) : null}
                    <PatientAgentActionList
                      patientId={patientId}
                      actionIds={actionIds}
                    />
                  </PatientAgentMessageList>
                </div>
              </section>
            </Sheet.Body>
            <Sheet.Footer>
              <PatientAgentComposer
                disabled={conversation.isPending}
                onSubmit={submit}
              />
            </Sheet.Footer>
          </Sheet.Dialog>
        </Sheet.Content>
      </Sheet.Backdrop>
    </Sheet>
  );
}

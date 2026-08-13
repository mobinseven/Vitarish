"use client";

import {
  ChatConversation,
  ChatLoader,
  ChatMessage,
  Sheet,
} from "@heroui-pro/react";
import { Button, Modal, Tooltip } from "@heroui/react";
import { FaIcon } from "@vitarish/web-ui";
import { AgentConversationComposer } from "@vitarish/intelligence-web";
import { type ReactNode, useEffect, useRef, useState } from "react";
import { useLocalizedText } from "../../localization";
import type { ClinicWorkspaceMessageResult } from "./hooks";
import { useClinicAssistantConversationSurface, useClinicWorkspaceConversation } from "./hooks";
import { ClinicWorkspaceActionList } from "./ClinicWorkspaceActions";
import {
  ClinicWorkspaceActivity,
  ClinicWorkspaceErrorState,
} from "./ClinicWorkspaceStates";

export {
  ClinicWorkspaceActivity,
  ClinicWorkspaceErrorState,
} from "./ClinicWorkspaceStates";

export function ClinicWorkspaceHeader({ patientName, onReset }: { patientName: string; onReset: () => void }) {
  const { t } = useLocalizedText();
  return (
    <>
      <div className="flex items-center justify-between gap-3">
        <Sheet.Heading>{t("clinic.clinicAssistant.title")}</Sheet.Heading>
        <Tooltip>
          <Button isIconOnly variant="tertiary" aria-label={t("clinic.clinicAssistant.actions.reset")} onPress={onReset}>
            <FaIcon icon="rotate-left" aria-hidden="true" />
          </Button>
          <Tooltip.Content>{t("clinic.clinicAssistant.actions.reset")}</Tooltip.Content>
        </Tooltip>
      </div>
      <p className="text-sm text-muted">
        {t("clinic.clinicAssistant.patient", { patient: patientName })}
      </p>
    </>
  );
}

export function ClinicWorkspaceComposer({
  disabled,
  onSubmit,
  scope = "patient",
  sessionKey,
}: {
  disabled: boolean;
  onSubmit: (message: string) => Promise<void>;
  scope?: "patient" | "operations";
  sessionKey?: string;
}) {
  const { locale, t } = useLocalizedText();
  const [message, setMessage] = useState("");
  const submit = async (nextMessage = message) => {
    const value = nextMessage.trim();
    if (!value || disabled) return;
    setMessage("");
    await onSubmit(value);
  };

  return (
    <AgentConversationComposer
      direction={locale === "fa" ? "rtl" : "ltr"}
      enabled={!disabled}
      isPending={disabled}
      label={scope === "operations" ? t("clinic.operationsAssistant.composer.label") : t("clinic.clinicAssistant.composer.label")}
      placeholder={scope === "operations" ? t("clinic.operationsAssistant.composer.placeholder") : t("clinic.clinicAssistant.composer.placeholder")}
      preferredLanguage={locale === "fa" ? "fa-IR" : "en-US"}
      sessionKey={sessionKey ?? `clinic.${scope}`}
      submitLabel={scope === "operations" ? t("clinic.operationsAssistant.composer.send") : t("clinic.clinicAssistant.composer.send")}
      value={message}
      onChange={setMessage}
      onSubmit={submit}
    />
  );
}

type ClinicWorkspaceChatMessage =
  ClinicWorkspaceMessageResult | { kind: "UserMessage"; content: string };

export function ClinicWorkspaceMessage({
  message,
  onRetry,
  scope = "patient",
}: {
  message: ClinicWorkspaceChatMessage;
  onRetry?: () => void;
  scope?: "patient" | "operations";
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
    return <ClinicWorkspaceErrorState onRetry={onRetry} scope={scope} />;
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
        alt={scope === "operations" ? t("clinic.operationsAssistant.title") : t("clinic.clinicAssistant.title")}
        fallback="AI"
        show
      />
      <ChatMessage.Body>
        <ChatMessage.Content>
          <p className="mb-1 text-xs font-semibold text-muted">
            {scope === "operations" ? t(`clinic.operationsAssistant.messages.${labels[message.kind]}`) : t(`clinic.clinicAssistant.messages.${labels[message.kind]}`)}
          </p>
          <p className="whitespace-pre-wrap">{message.content}</p>
        </ChatMessage.Content>
      </ChatMessage.Body>
    </ChatMessage.Assistant>
  );
}

export function ClinicWorkspaceMessageList({
  messages,
  onRetry,
  children,
  scope = "patient",
}: {
  messages: ClinicWorkspaceChatMessage[];
  onRetry?: () => void;
  children?: ReactNode;
  scope?: "patient" | "operations";
}) {
  const { t } = useLocalizedText();
  return (
    <ChatConversation
      className="min-h-0 flex-1"
      aria-label={scope === "operations" ? t("clinic.operationsAssistant.messages.label") : t("clinic.clinicAssistant.messages.label")}
    >
      <ChatConversation.Content className="mx-auto flex w-full max-w-3xl flex-col gap-6 px-4 py-5">
        {messages.map((message, index) => (
          <ClinicWorkspaceMessage
            key={`${message.kind}-${index}`}
            message={message}
            onRetry={message.kind === "Failure" ? onRetry : undefined}
            scope={scope}
          />
        ))}
        {children}
        <ChatConversation.ScrollAnchor />
      </ChatConversation.Content>
      <ChatConversation.ScrollButton
        aria-label={scope === "operations" ? t("clinic.operationsAssistant.messages.label") : t("clinic.clinicAssistant.messages.label")}
        tooltip={scope === "operations" ? t("clinic.operationsAssistant.messages.label") : t("clinic.clinicAssistant.messages.label")}
      />
    </ChatConversation>
  );
}

export function ClinicWorkspacePanel({
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
  const conversation = useClinicWorkspaceConversation(patientId, isOpen);
  const surface = useClinicAssistantConversationSurface(conversation, `${patientId}:${isOpen ? "open" : "closed"}`);
  const [confirmReset, setConfirmReset] = useState(false);
  const [resetError, setResetError] = useState(false);
  const focusTarget = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (surface.messages.length || surface.actionIds.length) focusTarget.current?.focus();
  }, [surface.messages.length, surface.actionIds.length]);

  const resetConversation = async () => {
    setResetError(false);
    try {
      await surface.deleteCurrent();
      setConfirmReset(false);
    } catch {
      setResetError(true);
    }
  };

  return (
    <Sheet isOpen={isOpen} placement="bottom" onOpenChange={onOpenChange}>
      <Sheet.Backdrop>
        <Sheet.Content className="mx-auto max-h-[96vh] max-w-3xl">
          <Sheet.Dialog className="flex h-[88vh] flex-col">
            <Sheet.Handle />
            <Sheet.Header>
              <ClinicWorkspaceHeader patientName={patientName} onReset={() => setConfirmReset(true)} />
              <Sheet.CloseTrigger aria-label={t("common.actions.close")}>
                <FaIcon icon="xmark" aria-hidden="true" />
              </Sheet.CloseTrigger>
            </Sheet.Header>
            <Sheet.Body className="flex min-h-0 flex-1 overflow-hidden p-0">
              <section
                dir={locale === "fa" ? "rtl" : "ltr"}
                className="flex min-h-0 flex-1 flex-col"
                aria-label={t("clinic.clinicAssistant.title")}
              >
                <div
                  ref={focusTarget}
                  tabIndex={-1}
                  className="flex min-h-0 flex-1 flex-col outline-none"
                >
                  <ClinicWorkspaceMessageList
                    messages={surface.messages}
                    onRetry={
                      surface.lastMessage
                        ? () => void surface.retry()
                        : undefined
                    }
                  >
                    {conversation.isPending ? (
                      <ChatMessage.Assistant>
                        <ChatMessage.Avatar
                          alt={t("clinic.clinicAssistant.title")}
                          fallback="AI"
                          show
                        />
                        <ChatMessage.Body>
                          <ChatLoader.Dots
                            label={t("clinic.clinicAssistant.running")}
                          />
                        </ChatMessage.Body>
                      </ChatMessage.Assistant>
                    ) : null}
                    {conversation.isError ? (
                      <ClinicWorkspaceErrorState
                        onRetry={
                          surface.lastMessage
                            ? () => void surface.retry()
                            : undefined
                        }
                      />
                    ) : null}
                    <ClinicWorkspaceActionList
                      patientId={patientId}
                      actionIds={surface.actionIds}
                    />
                  </ClinicWorkspaceMessageList>
                </div>
              </section>
            </Sheet.Body>
            <Sheet.Footer>
              <ClinicWorkspaceComposer
                disabled={conversation.isPending}
                onSubmit={surface.submit}
                sessionKey={`clinic.assistant.patient:${patientId}`}
              />
            </Sheet.Footer>
          </Sheet.Dialog>
        </Sheet.Content>
      </Sheet.Backdrop>
      <Modal isOpen={confirmReset} onOpenChange={setConfirmReset}>
        <Modal.Backdrop>
          <Modal.Container placement="center"><Modal.Dialog>
            <Modal.Header><Modal.Heading>{t("clinic.clinicAssistant.actions.resetTitle")}</Modal.Heading></Modal.Header>
            <Modal.Body><p>{t("clinic.clinicAssistant.actions.resetDescription")}</p>{resetError ? <p role="alert" className="text-danger">{t("clinic.clinicAssistant.actions.resetError")}</p> : null}</Modal.Body>
            <Modal.Footer>
              <Button variant="secondary" onPress={() => setConfirmReset(false)}>{t("common.actions.cancel")}</Button>
              <Button variant="danger" onPress={() => void resetConversation()}>{t("common.actions.delete")}</Button>
            </Modal.Footer>
          </Modal.Dialog></Modal.Container>
        </Modal.Backdrop>
      </Modal>
    </Sheet>
  );
}

"use client";

import { Button, TextArea, Tooltip } from "@heroui/react";
import { FaIcon } from "@vitarish/web-ui";
import { FormEvent, KeyboardEvent, useEffect, useRef, useState } from "react";
import { useLocalizedText } from "../../localization";
import type { PatientAgentMessageResult } from "./hooks";
import { usePatientAgentConversation } from "./hooks";
import { PatientAgentActionList } from "./PatientAgentActions";
import { PatientAgentActivity, PatientAgentErrorState } from "./PatientAgentStates";

export { PatientAgentActivity, PatientAgentErrorState } from "./PatientAgentStates";

export function PatientAgentHeader({ patientName }: { patientName: string }) {
  const { t } = useLocalizedText();
  return (
    <header>
      <h2 className="text-xl font-semibold">{t("clinic.patientAgent.title")}</h2>
      <p className="text-sm text-muted">{t("clinic.patientAgent.patient", { patient: patientName })}</p>
    </header>
  );
}

export function PatientAgentComposer({ disabled, onSubmit }: { disabled: boolean; onSubmit: (message: string) => Promise<void> }) {
  const { t } = useLocalizedText();
  const [message, setMessage] = useState("");
  const submit = async () => {
    const value = message.trim();
    if (!value || disabled) return;
    setMessage("");
    await onSubmit(value);
  };
  const onKeyDown = (event: KeyboardEvent<HTMLTextAreaElement>) => {
    if (event.key === "Enter" && !event.shiftKey) {
      event.preventDefault();
      void submit();
    }
  };
  return (
    <form onSubmit={(event: FormEvent) => { event.preventDefault(); void submit(); }} className="flex items-end gap-2">
      <TextArea
        aria-label={t("clinic.patientAgent.composer.label")}
        placeholder={t("clinic.patientAgent.composer.placeholder")}
        value={message}
        onChange={(event) => setMessage(event.target.value)}
        onKeyDown={onKeyDown}
        disabled={disabled}
        className="flex-1"
      />
      <Tooltip>
        <Button type="submit" variant="primary" isDisabled={disabled || !message.trim()} aria-label={t("clinic.patientAgent.composer.send")}>
          <FaIcon icon="paper-plane" aria-hidden="true" />{t("clinic.patientAgent.composer.send")}
        </Button>
        <Tooltip.Content>{t("clinic.patientAgent.composer.send")}</Tooltip.Content>
      </Tooltip>
    </form>
  );
}

export function PatientAgentMessage({ message }: { message: PatientAgentMessageResult }) {
  const { t } = useLocalizedText();
  const labels = {
    AssistantMessage: "assistant",
    InformationalToolResult: "information",
    MissingInformationRequest: "missing",
    Failure: "failure",
  } as const;
  return (
    <article className="rounded-lg border border-border p-3" data-kind={message.kind}>
      <p className="mb-1 text-xs font-semibold text-muted">{t(`clinic.patientAgent.messages.${labels[message.kind]}`)}</p>
      <p className="whitespace-pre-wrap">{message.content}</p>
    </article>
  );
}

export function PatientAgentMessageList({ messages }: { messages: PatientAgentMessageResult[] }) {
  const { t } = useLocalizedText();
  return (
    <div className="flex flex-col gap-2" aria-label={t("clinic.patientAgent.messages.label")}>
      {messages.map((message, index) => <PatientAgentMessage key={`${message.kind}-${index}`} message={message} />)}
    </div>
  );
}

export function PatientAgentPanel({ patientId, patientName }: { patientId: string; patientName: string }) {
  const { t, locale } = useLocalizedText();
  const conversation = usePatientAgentConversation(patientId);
  const [conversationId, setConversationId] = useState<string | null>(null);
  const [messages, setMessages] = useState<PatientAgentMessageResult[]>([]);
  const [actionIds, setActionIds] = useState<string[]>([]);
  const [lastMessage, setLastMessage] = useState("");
  const focusTarget = useRef<HTMLDivElement>(null);
  const activePatient = useRef(patientId);

  useEffect(() => {
    activePatient.current = patientId;
    setConversationId(null);
    setMessages([]);
    setActionIds([]);
    conversation.reset();
    // The mutation result is deliberately discarded whenever the trusted route patient changes.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [patientId]);

  useEffect(() => {
    if (messages.length || actionIds.length) focusTarget.current?.focus();
  }, [messages.length, actionIds.length]);

  const submit = async (message: string) => {
    const submittedPatient = patientId;
    setLastMessage(message);
    const result = await conversation.submit(message, conversationId);
    if (activePatient.current !== submittedPatient) return;
    setConversationId(result.conversationId);
    setMessages((current) => [...current, ...result.messages]);
    setActionIds((current) => [
      ...current,
      ...result.actions.map((action) => action.id).filter((id) => !current.includes(id)),
    ]);
  };

  return (
    <section dir={locale === "fa" ? "rtl" : "ltr"} className="space-y-4" aria-label={t("clinic.patientAgent.title")}>
      <PatientAgentHeader patientName={patientName} />
      <div ref={focusTarget} tabIndex={-1} className="space-y-3 outline-none">
        <PatientAgentMessageList messages={messages} />
        {conversation.isPending ? <PatientAgentActivity /> : null}
        {conversation.isError ? <PatientAgentErrorState onRetry={lastMessage ? () => void submit(lastMessage) : undefined} /> : null}
        <PatientAgentActionList patientId={patientId} actionIds={actionIds} />
      </div>
      <PatientAgentComposer disabled={conversation.isPending} onSubmit={submit} />
    </section>
  );
}

"use client";

import { ChatLoader, ChatMessage } from "@heroui-pro/react";
import { Button, Modal, Tooltip } from "@heroui/react";
import { FaIcon } from "@vitarish/ui";
import { useState } from "react";
import { useLocalizedText } from "../../localization";
import { OperationsAssistantActionList } from "./AssistantActions";
import { AssistantComposer, AssistantErrorState, AssistantMessageList } from "./AssistantPanel";
import { useClinicAssistantConversationSurface, useOperationsAssistantConversation } from "./hooks";

export function OperationsAssistantAside({ isOpen }: { isOpen: boolean }) {
  const { t, locale } = useLocalizedText();
  const conversation = useOperationsAssistantConversation(isOpen);
  const surface = useClinicAssistantConversationSurface(conversation, `clinic.assistant.operations:${isOpen ? "open" : "closed"}`);
  const [confirmReset, setConfirmReset] = useState(false);
  const [resetError, setResetError] = useState(false);
  const reset = async () => {
    setResetError(false);
    try {
      await surface.deleteCurrent();
      setConfirmReset(false);
    } catch {
      setResetError(true);
    }
  };

  return (
    <section dir={locale === "fa" ? "rtl" : "ltr"} className="flex h-full min-h-0 w-full flex-col" aria-label={t("clinic.operationsAssistant.title")}>
      <header className="flex shrink-0 items-center justify-between gap-3 border-b border-border px-4 py-3">
        <div><h2 className="font-semibold">{t("clinic.operationsAssistant.title")}</h2><p className="text-xs text-muted">{t("clinic.operationsAssistant.description")}</p></div>
        <Tooltip><Button isIconOnly variant="tertiary" aria-label={t("clinic.operationsAssistant.reset")} onPress={() => setConfirmReset(true)}><FaIcon icon="rotate-left" aria-hidden="true" /></Button><Tooltip.Content>{t("clinic.operationsAssistant.reset")}</Tooltip.Content></Tooltip>
      </header>
      <div className="flex min-h-0 flex-1 flex-col">
        <AssistantMessageList scope="operations" messages={surface.messages} onRetry={surface.lastMessage ? () => void surface.retry() : undefined}>
          {conversation.isPending ? <ChatMessage.Assistant><ChatMessage.Avatar show alt={t("clinic.operationsAssistant.title")} fallback="AI" /><ChatMessage.Body><ChatLoader.Dots label={t("clinic.operationsAssistant.running")} /></ChatMessage.Body></ChatMessage.Assistant> : null}
          {conversation.isError ? <AssistantErrorState scope="operations" onRetry={surface.lastMessage ? () => void surface.retry() : undefined} /> : null}
          <OperationsAssistantActionList actionIds={surface.actionIds} />
        </AssistantMessageList>
      </div>
      <footer className="shrink-0 border-t border-border p-3"><AssistantComposer scope="operations" disabled={conversation.isPending} onSubmit={surface.submit} sessionKey="clinic.assistant.operations" /></footer>
      <Modal.Backdrop isOpen={confirmReset} onOpenChange={setConfirmReset}><Modal.Container placement="center"><Modal.Dialog><Modal.Header><Modal.Heading>{t("clinic.operationsAssistant.resetTitle")}</Modal.Heading></Modal.Header><Modal.Body><p>{t("clinic.operationsAssistant.resetDescription")}</p>{resetError ? <p role="alert" className="text-danger">{t("clinic.operationsAssistant.resetError")}</p> : null}</Modal.Body><Modal.Footer><Button variant="tertiary" onPress={() => setConfirmReset(false)}><FaIcon icon="xmark" aria-hidden="true" />{t("common.actions.cancel")}</Button><Button variant="danger" onPress={() => void reset()}><FaIcon icon="trash" aria-hidden="true" />{t("common.actions.delete")}</Button></Modal.Footer></Modal.Dialog></Modal.Container></Modal.Backdrop>
    </section>
  );
}

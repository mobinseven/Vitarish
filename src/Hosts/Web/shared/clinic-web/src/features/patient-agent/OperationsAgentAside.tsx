"use client";

import { ChatLoader, ChatMessage } from "@heroui-pro/react";
import { Button, Modal, Tooltip } from "@heroui/react";
import { FaIcon } from "@vitarish/web-ui";
import { useState } from "react";
import { useLocalizedText } from "../../localization";
import { OperationsAgentActionList } from "./PatientAgentActions";
import { PatientAgentComposer, PatientAgentErrorState, PatientAgentMessageList } from "./PatientAgentPanel";
import { useClinicAgentConversationSurface, useOperationsAgentConversation } from "./hooks";

export function OperationsAgentAside({ isOpen }: { isOpen: boolean }) {
  const { t, locale } = useLocalizedText();
  const conversation = useOperationsAgentConversation(isOpen);
  const surface = useClinicAgentConversationSurface(conversation, `clinic.operations:${isOpen ? "open" : "closed"}`);
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
    <section dir={locale === "fa" ? "rtl" : "ltr"} className="flex h-full min-h-0 w-full flex-col" aria-label={t("clinic.operationsAgent.title")}>
      <header className="flex shrink-0 items-center justify-between gap-3 border-b border-border px-4 py-3">
        <div><h2 className="font-semibold">{t("clinic.operationsAgent.title")}</h2><p className="text-xs text-muted">{t("clinic.operationsAgent.description")}</p></div>
        <Tooltip><Button isIconOnly variant="tertiary" aria-label={t("clinic.operationsAgent.reset")} onPress={() => setConfirmReset(true)}><FaIcon icon="rotate-left" aria-hidden="true" /></Button><Tooltip.Content>{t("clinic.operationsAgent.reset")}</Tooltip.Content></Tooltip>
      </header>
      <div className="flex min-h-0 flex-1 flex-col">
        <PatientAgentMessageList scope="operations" messages={surface.messages} onRetry={surface.lastMessage ? () => void surface.retry() : undefined}>
          {conversation.isPending ? <ChatMessage.Assistant><ChatMessage.Avatar show alt={t("clinic.operationsAgent.title")} fallback="AI" /><ChatMessage.Body><ChatLoader.Dots label={t("clinic.operationsAgent.running")} /></ChatMessage.Body></ChatMessage.Assistant> : null}
          {conversation.isError ? <PatientAgentErrorState scope="operations" onRetry={surface.lastMessage ? () => void surface.retry() : undefined} /> : null}
          <OperationsAgentActionList actionIds={surface.actionIds} />
        </PatientAgentMessageList>
      </div>
      <footer className="shrink-0 border-t border-border p-3"><PatientAgentComposer scope="operations" disabled={conversation.isPending} onSubmit={surface.submit} sessionKey="clinic.operations" /></footer>
      <Modal isOpen={confirmReset} onOpenChange={setConfirmReset}><Modal.Backdrop><Modal.Container placement="center"><Modal.Dialog><Modal.Header><Modal.Heading>{t("clinic.operationsAgent.resetTitle")}</Modal.Heading></Modal.Header><Modal.Body><p>{t("clinic.operationsAgent.resetDescription")}</p>{resetError ? <p role="alert" className="text-danger">{t("clinic.operationsAgent.resetError")}</p> : null}</Modal.Body><Modal.Footer><Button variant="secondary" onPress={() => setConfirmReset(false)}><FaIcon icon="xmark" aria-hidden="true" />{t("common.actions.cancel")}</Button><Button variant="danger" onPress={() => void reset()}><FaIcon icon="trash" aria-hidden="true" />{t("common.actions.delete")}</Button></Modal.Footer></Modal.Dialog></Modal.Container></Modal.Backdrop></Modal>
    </section>
  );
}

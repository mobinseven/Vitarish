"use client";

import { ChatLoader, ChatMessage, ItemCard } from "@heroui-pro/react";
import { Button, Modal, Tooltip } from "@heroui/react";
import { FaIcon } from "@vitarish/web-ui";
import { formatDate } from "@vitarish/web-localization";
import { useEffect, useState } from "react";
import { clinicResolvePatientAgentCandidate } from "../../api/generated/clinic/clinic";
import { usePatientDetail } from "../../hooks/patients/usePatientDetail";
import { useLocalizedText } from "../../localization";
import { OperationsAgentActionList, PatientAgentActionList } from "./PatientAgentActions";
import { PatientAgentComposer, PatientAgentErrorState, PatientAgentMessageList } from "./PatientAgentPanel";
import { useClinicAgentConversationSurface, useOperationsAgentConversation, usePatientAgentConversation } from "./hooks";

export type ClinicAgentScope = { kind: "practice" } | { kind: "patient"; patientId: string };

export function ClinicAgentAside({
  isOpen,
  onNavigateToPatient,
  scope,
}: {
  isOpen: boolean;
  onNavigateToPatient: (patientId: string) => void;
  scope: ClinicAgentScope;
}) {
  const { locale, t } = useLocalizedText();
  const patientId = scope.kind === "patient" ? scope.patientId : "";
  const patientDetail = usePatientDetail(patientId);
  const patientConversation = usePatientAgentConversation(patientId, isOpen && scope.kind === "patient");
  const operationsConversation = useOperationsAgentConversation(isOpen && scope.kind === "practice");
  const conversation = scope.kind === "patient" ? patientConversation : operationsConversation;
  const surface = useClinicAgentConversationSurface(conversation, scope.kind === "patient" ? `patient:${patientId}` : "practice");
  const [confirmReset, setConfirmReset] = useState(false);
  const [selectionError, setSelectionError] = useState(false);
  const [resetError, setResetError] = useState(false);

  useEffect(() => {
    if (scope.kind !== "patient" || !isOpen) return;
    const raw = sessionStorage.getItem("clinic.pendingPatientRequest");
    if (!raw) return;
    try {
      const pending = JSON.parse(raw) as { patientId?: string; message?: string };
      if (pending.patientId !== patientId || !pending.message) return;
      sessionStorage.removeItem("clinic.pendingPatientRequest");
      void surface.submit(pending.message);
    } catch {
      sessionStorage.removeItem("clinic.pendingPatientRequest");
    }
  }, [isOpen, patientId, scope.kind, surface]);

  const selectCandidate = async (token: string) => {
    if (!surface.candidateRunId || !surface.candidateMessage) return;
    setSelectionError(false);
    try {
      const resolved = await clinicResolvePatientAgentCandidate({
        agentRunId: surface.candidateRunId,
        candidateToken: token,
      });
      sessionStorage.setItem("clinic.pendingPatientRequest", JSON.stringify({
        patientId: resolved.patientId,
        message: surface.candidateMessage,
      }));
      onNavigateToPatient(resolved.patientId);
    } catch {
      setSelectionError(true);
    }
  };

  const patient = scope.kind === "patient";
  const title = patient ? t("clinic.patientAgent.title") : t("clinic.operationsAgent.title");
  const description = patient ? t("clinic.patientAgent.patient", { patient: patientDetail.data?.name ?? patientId }) : t("clinic.operationsAgent.description");
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
    <section dir={locale === "fa" ? "rtl" : "ltr"} className="flex h-full min-h-0 w-full flex-col" aria-label={title}>
      <header className="flex shrink-0 items-center justify-between gap-3 border-b border-border px-4 py-3">
        <div><h2 className="font-semibold">{title}</h2><p className="text-xs text-muted">{description}</p></div>
        <Tooltip><Button isIconOnly variant="tertiary" aria-label={patient ? t("clinic.patientAgent.actions.reset") : t("clinic.operationsAgent.reset")} onPress={() => { setResetError(false); setConfirmReset(true); }}><FaIcon icon="rotate-left" aria-hidden="true" /></Button><Tooltip.Content>{patient ? t("clinic.patientAgent.actions.reset") : t("clinic.operationsAgent.reset")}</Tooltip.Content></Tooltip>
      </header>
      <div className="flex min-h-0 flex-1 flex-col">
        <PatientAgentMessageList scope={patient ? "patient" : "operations"} messages={surface.messages} onRetry={surface.lastMessage ? () => void surface.retry() : undefined}>
          {conversation.isPending ? <ChatMessage.Assistant><ChatMessage.Avatar show alt={title} fallback="AI" /><ChatMessage.Body><ChatLoader.Dots label={patient ? t("clinic.patientAgent.running") : t("clinic.operationsAgent.running")} /></ChatMessage.Body></ChatMessage.Assistant> : null}
          {conversation.isError ? <PatientAgentErrorState scope={patient ? "patient" : "operations"} onRetry={surface.lastMessage ? () => void surface.retry() : undefined} /> : null}
          {!patient && surface.candidates.length ? <div className="space-y-2 px-4" role="region" aria-label={t("clinic.operationsAgent.candidates.label")}>
            {surface.candidates.map((candidate) => <ItemCard key={candidate.candidateToken} variant="secondary">
              <ItemCard.Icon><FaIcon icon="user" aria-hidden="true" /></ItemCard.Icon>
              <ItemCard.Content><ItemCard.Title>{candidate.displayName}</ItemCard.Title><ItemCard.Description>{[candidate.maskedIdentification, candidate.maskedPhone, candidate.dateOfBirth ? formatDate(candidate.dateOfBirth, locale) : null].filter(Boolean).join(" · ")}</ItemCard.Description></ItemCard.Content>
              <ItemCard.Action><Tooltip><Button size="sm" variant="outline" aria-label={t("clinic.operationsAgent.candidates.select")} onPress={() => void selectCandidate(candidate.candidateToken)}><FaIcon icon="arrow-right" aria-hidden="true" />{t("clinic.operationsAgent.candidates.select")}</Button><Tooltip.Content>{t("clinic.operationsAgent.candidates.select")}</Tooltip.Content></Tooltip></ItemCard.Action>
            </ItemCard>)}
            {selectionError ? <p role="alert" className="text-sm text-danger">{t("clinic.operationsAgent.candidates.expired")}</p> : null}
          </div> : null}
          {patient ? <PatientAgentActionList patientId={patientId} actionIds={surface.actionIds} /> : <OperationsAgentActionList actionIds={surface.actionIds} />}
        </PatientAgentMessageList>
      </div>
      <footer className="shrink-0 border-t border-border p-3"><PatientAgentComposer scope={patient ? "patient" : "operations"} disabled={conversation.isPending} onSubmit={surface.submit} sessionKey={patient ? `clinic.patient:${patientId}` : "clinic.operations"} /></footer>
      <Modal isOpen={confirmReset} onOpenChange={setConfirmReset}><Modal.Backdrop><Modal.Container placement="center"><Modal.Dialog><Modal.Header><Modal.Heading>{patient ? t("clinic.patientAgent.actions.resetTitle") : t("clinic.operationsAgent.resetTitle")}</Modal.Heading></Modal.Header><Modal.Body><p>{patient ? t("clinic.patientAgent.actions.resetDescription") : t("clinic.operationsAgent.resetDescription")}</p>{resetError ? <p role="alert" className="text-sm text-danger">{patient ? t("clinic.patientAgent.actions.resetError") : t("clinic.operationsAgent.resetError")}</p> : null}</Modal.Body><Modal.Footer><Button variant="secondary" onPress={() => setConfirmReset(false)}><FaIcon icon="xmark" aria-hidden="true" />{t("common.actions.cancel")}</Button><Button variant="danger" onPress={() => void reset()}><FaIcon icon="trash" aria-hidden="true" />{t("common.actions.delete")}</Button></Modal.Footer></Modal.Dialog></Modal.Container></Modal.Backdrop></Modal>
    </section>
  );
}

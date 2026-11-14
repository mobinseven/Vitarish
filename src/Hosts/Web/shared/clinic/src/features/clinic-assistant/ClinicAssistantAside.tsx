"use client";

import { ChatLoader, ChatMessage } from "@heroui-pro/react";
import { Button, Modal, Tooltip } from "@heroui/react";
import { FaIcon } from "@vitarish/ui";
import { useAuthSession } from "@vitarish/auth";
import { useEffect, useState } from "react";
import { clinicResolveClinicAssistantCandidate } from "../../api/generated/clinic/clinic";
import { usePatientDetail } from "../../hooks/patients/usePatientDetail";
import { useLocalizedText } from "../../localization";
import { AssistantActionList } from "./AssistantActions";
import { AssistantCandidateList } from "./AssistantCandidateList";
import { OperationsGuidedReply } from "./OperationsGuidedReply";
import type { NavigationIntent } from "../../navigation";
import { isSetupGoal, setupGoalIntents } from "../clinic-setup/setupGoals";
import { AssistantComposer, AssistantErrorState, AssistantMessageList } from "./AssistantPanel";
import { operationsComposerSessionKey, operationsSurfaceMemoryKey, patientComposerSessionKey, useClinicAssistantConversationSurface, useOperationsAssistantConversation, useAssistantConversation } from "./hooks";

export type ClinicAssistantScope = { kind: "practice" } | { kind: "patient"; patientId: string };

export function ClinicAssistantAside({
  isOpen,
  onNavigateToPatient,
  onNavigate,
  scope,
}: {
  isOpen: boolean;
  onNavigateToPatient: (patientId: string) => void;
  /** Opens a clinic screen the assistant suggests, such as a setup step's screen. */
  onNavigate?: (intent: NavigationIntent) => void;
  scope: ClinicAssistantScope;
}) {
  const { locale, t } = useLocalizedText();
  const patientId = scope.kind === "patient" ? scope.patientId : "";
  const patientDetail = usePatientDetail(patientId);
  const patientConversation = useAssistantConversation(patientId, isOpen && scope.kind === "patient");
  const operationsConversation = useOperationsAssistantConversation(isOpen && scope.kind === "practice");
  const conversation = scope.kind === "patient" ? patientConversation : operationsConversation;
  const { user } = useAuthSession();
  // The practice conversation's messages are shared with the assistant page through memory.
  const surface = useClinicAssistantConversationSurface(
    conversation,
    scope.kind === "patient" ? `patient:${patientId}` : "practice",
    scope.kind === "practice" && user?.userId ? operationsSurfaceMemoryKey(user.userId) : undefined,
  );
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
      const resolved = await clinicResolveClinicAssistantCandidate({
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
  const title = patient ? t("clinic.clinicAssistant.title") : t("clinic.operationsAssistant.title");
  const description = patient ? t("clinic.clinicAssistant.patient", { patient: patientDetail.data?.name ?? patientId }) : t("clinic.operationsAssistant.description");
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
        <Tooltip><Button isIconOnly variant="tertiary" aria-label={patient ? t("clinic.clinicAssistant.actions.reset") : t("clinic.operationsAssistant.reset")} onPress={() => { setResetError(false); setConfirmReset(true); }}><FaIcon icon="rotate-left" aria-hidden="true" /></Button><Tooltip.Content>{patient ? t("clinic.clinicAssistant.actions.reset") : t("clinic.operationsAssistant.reset")}</Tooltip.Content></Tooltip>
      </header>
      <div className="flex min-h-0 flex-1 flex-col">
        <AssistantMessageList scope={patient ? "patient" : "operations"} messages={surface.messages} onRetry={surface.lastMessage ? () => void surface.retry() : undefined}>
          {conversation.isPending ? <ChatMessage.Assistant><ChatMessage.Avatar show alt={title} fallback="AI" /><ChatMessage.Body><ChatLoader.Dots label={patient ? t("clinic.clinicAssistant.running") : t("clinic.operationsAssistant.running")} /></ChatMessage.Body></ChatMessage.Assistant> : null}
          {conversation.isError ? <AssistantErrorState scope={patient ? "patient" : "operations"} onRetry={surface.lastMessage ? () => void surface.retry() : undefined} /> : null}
          {!patient ? <AssistantCandidateList candidates={surface.candidates} onSelect={(token) => void selectCandidate(token)} hasError={selectionError} /> : null}
          {patient ? <AssistantActionList patientId={patientId} actionIds={surface.actionIds} /> : (
            <OperationsGuidedReply
              actionGroups={surface.actionGroups}
              question={surface.question}
              nextAction={surface.nextAction}
              turnKey={surface.lastMessage}
              isPending={conversation.isPending}
              submit={(message) => void surface.submit(message)}
              replaceAction={surface.replaceAction}
              onOpenGoal={onNavigate ? (goal) => { if (isSetupGoal(goal)) onNavigate(setupGoalIntents[goal]); } : undefined}
              showDismissedSetupGoals
            />
          )}
        </AssistantMessageList>
      </div>
      <footer className="shrink-0 border-t border-border p-3"><AssistantComposer scope={patient ? "patient" : "operations"} disabled={conversation.isPending} onSubmit={surface.submit} sessionKey={patient ? patientComposerSessionKey(patientId) : operationsComposerSessionKey} /></footer>
      <Modal.Backdrop isOpen={confirmReset} onOpenChange={setConfirmReset}><Modal.Container placement="center"><Modal.Dialog><Modal.Header><Modal.Heading>{patient ? t("clinic.clinicAssistant.actions.resetTitle") : t("clinic.operationsAssistant.resetTitle")}</Modal.Heading></Modal.Header><Modal.Body><p>{patient ? t("clinic.clinicAssistant.actions.resetDescription") : t("clinic.operationsAssistant.resetDescription")}</p>{resetError ? <p role="alert" className="text-sm text-danger">{patient ? t("clinic.clinicAssistant.actions.resetError") : t("clinic.operationsAssistant.resetError")}</p> : null}</Modal.Body><Modal.Footer><Button variant="tertiary" onPress={() => setConfirmReset(false)}><FaIcon icon="xmark" aria-hidden="true" />{t("common.actions.cancel")}</Button><Button variant="danger" onPress={() => void reset()}><FaIcon icon="trash" aria-hidden="true" />{t("common.actions.delete")}</Button></Modal.Footer></Modal.Dialog></Modal.Container></Modal.Backdrop>
    </section>
  );
}

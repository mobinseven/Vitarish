"use client";

import { ChatLoader, ChatMessage, PromptSuggestion } from "@heroui-pro/react";
import { Alert, Button, Chip, Separator, Tooltip } from "@heroui/react";
import { useAuthSession } from "@vitarish/auth";
import { AgentChatWorkspace, ConfirmDialog, FaIcon, PageHeader } from "@vitarish/ui";
import { useCallback, useEffect, useRef, useState } from "react";
import { clinicResolveClinicAssistantCandidate } from "../../api/generated/clinic/clinic";
import { useLocalizedText } from "../../localization";
import type { NavigationIntent } from "../../navigation";
import { isSetupGoal, setupGoalIntents } from "../clinic-setup/setupGoals";
import { AssistantActionList } from "./AssistantActions";
import { AssistantCandidateList } from "./AssistantCandidateList";
import { AssistantComposer, AssistantErrorState, AssistantMessage, AssistantMessageList } from "./AssistantPanel";
import { OperationsGuidedReply } from "./OperationsGuidedReply";
import {
  forgetClinicAssistantSurface,
  hasStoredOperationsConversation,
  operationsComposerSessionKey,
  patientComposerSessionKey,
  operationsSurfaceMemoryKey,
  readClinicAssistantSurface,
  useAssistantConversation,
  useClinicAssistantConversationSurface,
  useOperationsAssistantConversation,
  type ClinicAssistantSurfaceSnapshot,
} from "./hooks";

type PatientFocus = { patientId: string; name: string };
type SegmentScope = { kind: "practice" } | ({ kind: "patient" } & PatientFocus);
/** A finished stretch of the timeline: what one conversation showed before the page moved to another. */
type TimelineSegment = { id: string; scope: SegmentScope; snapshot: ClinicAssistantSurfaceSnapshot };
type Timeline = { segments: TimelineSegment[]; focus: (PatientFocus & { segmentId: string }) | null };

const emptyTimeline: Timeline = { segments: [], focus: null };
// In memory only, per signed-in user: clinical text never goes to browser storage, and a reload clears it.
const timelineMemory = new Map<string, Timeline>();
const patientSurfaceMemoryKey = (userId: string, segmentId: string) => `${userId}:clinic.assistant.page:${segmentId}`;

/** Conversation starters; each one maps to a tool the operations assistant has. */
const suggestions = [
  { key: "todaySchedule", icon: "calendar-day" },
  { key: "freeSlots", icon: "calendar-plus" },
  { key: "findPatient", icon: "magnifying-glass" },
  { key: "bookAppointment", icon: "calendar-check" },
  { key: "addShift", icon: "clock" },
  { key: "inviteStaff", icon: "user-plus" },
] as const;

const hasContent = (snapshot: ClinicAssistantSurfaceSnapshot) =>
  snapshot.messages.length > 0 || snapshot.actionIds.length > 0 || snapshot.actionGroups.length > 0;

let segmentSequence = 0;
const nextSegmentId = () => `segment-${Date.now().toString(36)}-${(segmentSequence += 1)}`;

export interface ClinicAssistantWorkspaceProps {
  /** Opens a clinic screen the assistant suggests, such as a setup step's screen. */
  onNavigate?: (intent: NavigationIntent) => void;
  /** Opens a patient's record. */
  onOpenPatient: (patientId: string) => void;
}

/**
 * The clinic assistant as a page: one timeline that continues the practice operations conversation (shared with the
 * side panel) and moves into a patient's conversation when a patient is chosen from the assistant's search results.
 */
export function ClinicAssistantWorkspace(props: ClinicAssistantWorkspaceProps) {
  const { user } = useAuthSession();
  if (!user?.userId) return null;
  // A different person starts from their own memory.
  return <Workspace key={user.userId} userId={user.userId} {...props} />;
}

function Workspace({ userId, onNavigate, onOpenPatient }: ClinicAssistantWorkspaceProps & { userId: string }) {
  const { t, locale } = useLocalizedText();
  const operationsMemoryKey = operationsSurfaceMemoryKey(userId);
  const [timeline, setTimeline] = useState<Timeline>(() => timelineMemory.get(userId) ?? emptyTimeline);
  useEffect(() => {
    timelineMemory.set(userId, timeline);
  }, [timeline, userId]);
  const focus = timeline.focus;

  const operationsConversation = useOperationsAssistantConversation(true);
  const operations = useClinicAssistantConversationSurface(operationsConversation, "practice", operationsMemoryKey);
  // Closing keeps the last patient, so the conversation that closes is deleted under its own patient.
  const lastPatientId = useRef("");
  if (focus) lastPatientId.current = focus.patientId;
  const patientConversation = useAssistantConversation(lastPatientId.current, focus !== null);
  const patient = useClinicAssistantConversationSurface(
    patientConversation,
    focus ? `patient:${focus.segmentId}` : "patient:none",
    focus ? patientSurfaceMemoryKey(userId, focus.segmentId) : undefined,
  );
  const active = focus ? patient : operations;
  const activeConversation = focus ? patientConversation : operationsConversation;

  // After a reload the server conversation continues, but its earlier messages cannot be shown yet.
  const [continuesEarlier, setContinuesEarlier] = useState(
    () => hasStoredOperationsConversation()
      && !readClinicAssistantSurface(operationsMemoryKey)?.messages.length
      && !timelineMemory.get(userId)?.segments.length,
  );
  const [selectionError, setSelectionError] = useState(false);
  const [confirmNew, setConfirmNew] = useState(false);
  const [newError, setNewError] = useState(false);
  const [isStartingNew, setStartingNew] = useState(false);

  // The request that found a patient continues in that patient's conversation once it is open.
  const [handoff, setHandoff] = useState<string | null>(null);
  const patientSubmit = patient.submit;
  useEffect(() => {
    if (!focus || handoff === null) return;
    setHandoff(null);
    void patientSubmit(handoff);
  }, [focus, handoff, patientSubmit]);

  const submit = useCallback(async (message: string) => {
    setContinuesEarlier(false);
    // Review cards stay with the turn that produced them: what this stretch already shows moves into the timeline,
    // so the new turn appears below it.
    const shown = active.snapshot;
    if (shown.actionIds.length > 0 || shown.actionGroups.length > 0) {
      const scope: SegmentScope = focus ? { kind: "patient", patientId: focus.patientId, name: focus.name } : { kind: "practice" };
      setTimeline((current) => ({ ...current, segments: [...current.segments, { id: nextSegmentId(), scope, snapshot: shown }] }));
      active.clearView();
    }
    await active.submit(message);
  }, [active, focus]);

  const selectCandidate = async (candidateToken: string) => {
    if (!operations.candidateRunId || !operations.candidateMessage) return;
    setSelectionError(false);
    try {
      const resolved = await clinicResolveClinicAssistantCandidate({
        agentRunId: operations.candidateRunId,
        candidateToken,
      });
      const name = operations.candidates.find((candidate) => candidate.candidateToken === candidateToken)?.displayName ?? "";
      const finished = operations.snapshot;
      setTimeline((current) => ({
        segments: hasContent(finished)
          ? [...current.segments, { id: nextSegmentId(), scope: { kind: "practice" }, snapshot: finished }]
          : current.segments,
        focus: { patientId: resolved.patientId, name, segmentId: nextSegmentId() },
      }));
      setHandoff(operations.candidateMessage);
      operations.clearView();
    } catch {
      setSelectionError(true);
    }
  };

  const clearFocus = () => {
    if (!focus) return;
    patient.cancel();
    const finished = patient.snapshot;
    forgetClinicAssistantSurface(patientSurfaceMemoryKey(userId, focus.segmentId));
    setTimeline((current) => ({
      segments: hasContent(finished)
        ? [...current.segments, { id: focus.segmentId, scope: { kind: "patient", patientId: focus.patientId, name: focus.name }, snapshot: finished }]
        : current.segments,
      focus: null,
    }));
  };

  const startNew = async () => {
    setNewError(false);
    setStartingNew(true);
    try {
      if (focus) forgetClinicAssistantSurface(patientSurfaceMemoryKey(userId, focus.segmentId));
      await operations.deleteCurrent();
      setTimeline(emptyTimeline);
      setContinuesEarlier(false);
      setConfirmNew(false);
    } catch {
      setNewError(true);
    } finally {
      setStartingNew(false);
    }
  };

  /** A revised proposal takes the original's place, also in an earlier stretch of the timeline. */
  const replaceInSegment = (segmentId: string) => (originalId: string, replacementId: string) =>
    setTimeline((current) => ({
      ...current,
      segments: current.segments.map((segment) => segment.id !== segmentId ? segment : {
        ...segment,
        snapshot: {
          ...segment.snapshot,
          actionGroups: segment.snapshot.actionGroups.map((group) => ({
            ...group,
            actionIds: group.actionIds.map((id) => (id === originalId ? replacementId : id)),
          })),
          actionIds: segment.snapshot.actionIds.map((id) => (id === originalId ? replacementId : id)),
        },
      }),
    }));

  const openGoal = onNavigate ? (goal: string) => { if (isSetupGoal(goal)) onNavigate(setupGoalIntents[goal]); } : undefined;
  const liveScope: SegmentScope = focus ? { kind: "patient", patientId: focus.patientId, name: focus.name } : { kind: "practice" };
  const isEmpty = !timeline.segments.length && !hasContent(active.snapshot) && !activeConversation.isPending;
  const canStartNew = !isEmpty || continuesEarlier || focus !== null;
  const title = t("clinic.assistantWorkspace.title");

  const scopeDivider = (scope: SegmentScope, previous: SegmentScope | undefined) => {
    if (previous && previous.kind === scope.kind && (scope.kind === "practice" || (previous.kind === "patient" && previous.patientId === scope.patientId))) return null;
    if (scope.kind === "patient") return <TimelineDivider icon="user" label={t("clinic.assistantWorkspace.nowAbout", { patient: scope.name })} />;
    if (previous?.kind === "patient") return <TimelineDivider icon="house-medical" label={t("clinic.assistantWorkspace.backToClinic")} />;
    return null;
  };

  return (
    <div dir={locale === "fa" ? "rtl" : "ltr"} className="flex min-h-0 flex-1 flex-col">
      <AgentChatWorkspace
        header={
          <PageHeader
            icon="sparkles"
            title={title}
            details={t("clinic.assistantWorkspace.description")}
            actions={
              <>
                {focus ? (
                  <div className="flex items-center gap-1" role="group" aria-label={t("clinic.assistantWorkspace.focus.label")}>
                    <Chip color="accent" variant="soft">
                      <FaIcon icon="user" aria-hidden="true" />
                      <Chip.Label>{t("clinic.assistantWorkspace.focus.patient", { patient: focus.name })}</Chip.Label>
                    </Chip>
                    <Tooltip>
                      <Button isIconOnly size="sm" variant="tertiary" aria-label={t("clinic.assistantWorkspace.focus.openRecord")} onPress={() => onOpenPatient(focus.patientId)}>
                        <FaIcon icon="folder-open" aria-hidden="true" />
                      </Button>
                      <Tooltip.Content>{t("clinic.assistantWorkspace.focus.openRecord")}</Tooltip.Content>
                    </Tooltip>
                    <Tooltip>
                      <Button isIconOnly size="sm" variant="tertiary" aria-label={t("clinic.assistantWorkspace.focus.clear")} onPress={clearFocus}>
                        <FaIcon icon="xmark" aria-hidden="true" />
                      </Button>
                      <Tooltip.Content>{t("clinic.assistantWorkspace.focus.clear")}</Tooltip.Content>
                    </Tooltip>
                  </div>
                ) : null}
                <Tooltip>
                  <Button
                    variant="secondary"
                    aria-label={t("clinic.assistantWorkspace.newConversation")}
                    isDisabled={!canStartNew || activeConversation.isPending}
                    onPress={() => { setNewError(false); setConfirmNew(true); }}
                  >
                    <FaIcon icon="pen-to-square" aria-hidden="true" />
                    <span className="hidden sm:inline">{t("clinic.assistantWorkspace.newConversation")}</span>
                  </Button>
                  <Tooltip.Content>{t("clinic.assistantWorkspace.newConversation")}</Tooltip.Content>
                </Tooltip>
              </>
            }
          />
        }
        notice={continuesEarlier ? (
          <Alert status="accent">
            <Alert.Indicator />
            <Alert.Content>
              <Alert.Title>{t("clinic.assistantWorkspace.continuing.title")}</Alert.Title>
              <Alert.Description>{t("clinic.assistantWorkspace.continuing.description")}</Alert.Description>
            </Alert.Content>
          </Alert>
        ) : null}
        composer={
          <AssistantComposer
            scope={focus ? "patient" : "operations"}
            disabled={activeConversation.isPending}
            onSubmit={submit}
            sessionKey={focus ? patientComposerSessionKey(focus.patientId) : operationsComposerSessionKey}
            actions={activeConversation.isPending ? (
              <Tooltip>
                <Button isIconOnly size="sm" variant="tertiary" aria-label={t("clinic.assistantWorkspace.stop")} onPress={active.cancel}>
                  <FaIcon icon="stop" aria-hidden="true" />
                </Button>
                <Tooltip.Content>{t("clinic.assistantWorkspace.stop")}</Tooltip.Content>
              </Tooltip>
            ) : null}
          />
        }
      >
        <AssistantMessageList scope="operations" messages={[]}>
          {isEmpty ? <Suggestions onPick={(message) => void submit(message)} /> : null}
          {timeline.segments.map((segment, index) => (
            <div key={segment.id} className="flex flex-col gap-6">
              {scopeDivider(segment.scope, timeline.segments[index - 1]?.scope)}
              <MessageRun snapshot={segment.snapshot} scope={segment.scope.kind === "patient" ? "patient" : "operations"} />
              {segment.scope.kind === "patient" ? (
                <AssistantActionList patientId={segment.scope.patientId} actionIds={segment.snapshot.actionIds} />
              ) : (
                <OperationsGuidedReply
                  actionGroups={segment.snapshot.actionGroups}
                  question={null}
                  nextAction={null}
                  turnKey={segment.id}
                  isPending={false}
                  submit={(message) => void submit(message)}
                  replaceAction={replaceInSegment(segment.id)}
                />
              )}
            </div>
          ))}
          {timeline.segments.length ? scopeDivider(liveScope, timeline.segments.at(-1)?.scope) : focus ? scopeDivider(liveScope, undefined) : null}
          <MessageRun snapshot={active.snapshot} scope={focus ? "patient" : "operations"} onRetry={active.lastMessage ? () => void active.retry() : undefined} />
          {activeConversation.isPending ? (
            <ChatMessage.Assistant>
              <ChatMessage.Avatar show alt={title} fallback="AI" />
              <ChatMessage.Body><ChatLoader.Dots label={t("clinic.assistantWorkspace.running")} /></ChatMessage.Body>
            </ChatMessage.Assistant>
          ) : null}
          {activeConversation.isError ? <AssistantErrorState scope={focus ? "patient" : "operations"} onRetry={active.lastMessage ? () => void active.retry() : undefined} /> : null}
          {focus ? (
            <AssistantActionList patientId={focus.patientId} actionIds={patient.actionIds} />
          ) : (
            <>
              <AssistantCandidateList candidates={operations.candidates} hasError={selectionError} onSelect={(token) => void selectCandidate(token)} />
              <OperationsGuidedReply
                actionGroups={operations.actionGroups}
                question={operations.question}
                nextAction={operations.nextAction}
                turnKey={operations.lastMessage}
                isPending={operationsConversation.isPending}
                submit={(message) => void submit(message)}
                replaceAction={operations.replaceAction}
                onOpenGoal={openGoal}
                showDismissedSetupGoals
              />
            </>
          )}
        </AssistantMessageList>
      </AgentChatWorkspace>
      <ConfirmDialog
        isOpen={confirmNew}
        isPending={isStartingNew}
        title={t("clinic.assistantWorkspace.newConversationTitle")}
        message={<>
          <p>{t("clinic.assistantWorkspace.newConversationDescription")}</p>
          {newError ? <p role="alert" className="text-sm text-danger">{t("clinic.assistantWorkspace.newConversationError")}</p> : null}
        </>}
        confirmLabel={t("clinic.assistantWorkspace.newConversation")}
        confirmIcon="pen-to-square"
        cancelLabel={t("common.actions.cancel")}
        onCancel={() => setConfirmNew(false)}
        onConfirm={() => void startNew()}
      />
    </div>
  );
}

function MessageRun({ snapshot, scope, onRetry }: { snapshot: ClinicAssistantSurfaceSnapshot; scope: "patient" | "operations"; onRetry?: () => void }) {
  return (
    <>
      {snapshot.messages.map((message, index) => (
        <AssistantMessage
          key={`${message.kind}-${index}`}
          message={message}
          scope={scope}
          copyable
          onRetry={message.kind === "Failure" ? onRetry : undefined}
        />
      ))}
    </>
  );
}

function TimelineDivider({ icon, label }: { icon: string; label: string }) {
  return (
    <div className="flex items-center gap-3 text-sm text-muted" role="separator" aria-label={label}>
      <Separator className="flex-1" />
      <span className="flex items-center gap-2"><FaIcon icon={icon} aria-hidden="true" />{label}</span>
      <Separator className="flex-1" />
    </div>
  );
}

function Suggestions({ onPick }: { onPick: (message: string) => void }) {
  const { t } = useLocalizedText();
  const once = useRef(false);
  return (
    <PromptSuggestion>
      <PromptSuggestion.Header>
        <PromptSuggestion.Title>{t("clinic.assistantWorkspace.suggestions.title")}</PromptSuggestion.Title>
        <PromptSuggestion.Description>{t("clinic.assistantWorkspace.suggestions.description")}</PromptSuggestion.Description>
      </PromptSuggestion.Header>
      <PromptSuggestion.Items>
        {suggestions.map(({ key, icon }) => (
          <PromptSuggestion.Item
            key={key}
            onPress={() => {
              if (once.current) return;
              once.current = true;
              onPick(t(`clinic.assistantWorkspace.suggestions.items.${key}.prompt`));
            }}
          >
            <FaIcon icon={icon} aria-hidden="true" className="shrink-0 text-muted" />
            <span className="flex min-w-0 flex-col items-start gap-0.5 text-start">
              <span className="font-medium">{t(`clinic.assistantWorkspace.suggestions.items.${key}.title`)}</span>
              <span className="text-sm text-muted">{t(`clinic.assistantWorkspace.suggestions.items.${key}.description`)}</span>
            </span>
          </PromptSuggestion.Item>
        ))}
      </PromptSuggestion.Items>
    </PromptSuggestion>
  );
}

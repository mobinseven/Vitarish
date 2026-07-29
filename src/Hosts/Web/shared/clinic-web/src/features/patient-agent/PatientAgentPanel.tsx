"use client";

import { Button, Card, TextArea, Tooltip } from "@heroui/react";
import { FaIcon } from "@vitarish/web-ui";
import { FormEvent, KeyboardEvent, useEffect, useId, useRef, useState } from "react";
import { formatDate, formatDateTime, formatNumber, useLocalizedText } from "../../localization";
import type { PatientAgentAction, PatientAgentMessageResult } from "./hooks";
import { useConfirmPatientAgentAction, usePatientAgentAction, usePatientAgentConversation, useRejectPatientAgentAction } from "./hooks";

type UnknownRecord = Record<string, unknown>;
const record = (value: unknown): UnknownRecord => value && typeof value === "object" && !Array.isArray(value) ? value as UnknownRecord : {};
const text = (value: unknown): string | null => typeof value === "string" && value.trim() ? value : null;
const number = (value: unknown): number | null => typeof value === "number" ? value : null;
const list = (value: unknown): unknown[] => Array.isArray(value) ? value : [];

export function PatientAgentHeader({ patientName }: { patientName: string }) {
  const { t } = useLocalizedText();
  return <header><h2 className="text-xl font-semibold">{t("clinic.patientAgent.title")}</h2><p className="text-sm text-muted">{t("clinic.patientAgent.patient", { patient: patientName })}</p></header>;
}

export function PatientAgentComposer({ disabled, onSubmit }: { disabled: boolean; onSubmit: (message: string) => Promise<void> }) {
  const { t } = useLocalizedText();
  const [message, setMessage] = useState("");
  const submit = async () => { const value = message.trim(); if (!value || disabled) return; setMessage(""); await onSubmit(value); };
  const onKeyDown = (event: KeyboardEvent<HTMLTextAreaElement>) => { if (event.key === "Enter" && !event.shiftKey) { event.preventDefault(); void submit(); } };
  return <form onSubmit={(event: FormEvent) => { event.preventDefault(); void submit(); }} className="flex items-end gap-2">
    <TextArea aria-label={t("clinic.patientAgent.composer.label")} placeholder={t("clinic.patientAgent.composer.placeholder")} value={message} onChange={(event) => setMessage(event.target.value)} onKeyDown={onKeyDown} disabled={disabled} className="flex-1" />
    <Tooltip><Button type="submit" variant="primary" isDisabled={disabled || !message.trim()} aria-label={t("clinic.patientAgent.composer.send")}><FaIcon icon="paper-plane" aria-hidden="true" />{t("clinic.patientAgent.composer.send")}</Button><Tooltip.Content>{t("clinic.patientAgent.composer.send")}</Tooltip.Content></Tooltip>
  </form>;
}

export function PatientAgentMessage({ message }: { message: PatientAgentMessageResult }) {
  const { t } = useLocalizedText();
  const labels = { AssistantMessage: "assistant", InformationalToolResult: "information", MissingInformationRequest: "missing", Failure: "failure" } as const;
  return <article className="rounded-lg border border-border p-3" data-kind={message.kind}><p className="mb-1 text-xs font-semibold text-muted">{t(`clinic.patientAgent.messages.${labels[message.kind]}`)}</p><p className="whitespace-pre-wrap">{message.content}</p></article>;
}

export function PatientAgentMessageList({ messages }: { messages: PatientAgentMessageResult[] }) {
  const { t } = useLocalizedText();
  return <div className="flex flex-col gap-2" aria-label={t("clinic.patientAgent.messages.label")}>{messages.map((message, index) => <PatientAgentMessage key={`${message.kind}-${index}`} message={message} />)}</div>;
}

export function PatientAgentActivity() { const { t } = useLocalizedText(); return <p role="status" className="text-sm text-muted"><FaIcon icon="spinner" className="me-2" aria-hidden="true" />{t("clinic.patientAgent.running")}</p>; }
export function PatientAgentErrorState({ onRetry }: { onRetry?: () => void }) { const { t } = useLocalizedText(); return <div role="alert" className="rounded-lg border border-danger p-3"><p>{t("clinic.patientAgent.error")}</p>{onRetry ? <Tooltip><Button variant="outline" onPress={onRetry}><FaIcon icon="rotate-right" aria-hidden="true" />{t("clinic.patientAgent.retry")}</Button><Tooltip.Content>{t("clinic.patientAgent.retry")}</Tooltip.Content></Tooltip> : null}</div>; }

export function PatientAgentActionStatus({ status, completionId, failure }: { status: string; completionId?: string | null; failure?: string | null }) {
  const { t } = useLocalizedText();
  return <div role="status" aria-live="polite" className="text-sm font-medium"><FaIcon icon={status === "Completed" ? "circle-check" : status === "Failed" ? "triangle-exclamation" : "circle-info"} className="me-2" aria-hidden="true" />{t(`clinic.patientAgent.status.${status.toLowerCase()}`)}{completionId ? <span className="ms-2 font-normal">{t("clinic.patientAgent.completionId", { id: completionId })}</span> : null}{failure ? <p className="mt-1 font-normal text-danger">{failure}</p> : null}</div>;
}

function Field({ label, value, assertion = false }: { label: string; value: unknown; assertion?: boolean }) {
  if (value == null || value === "" || (Array.isArray(value) && !value.length)) return null;
  const rendered = Array.isArray(value) ? value.join(", ") : String(value);
  return <div className={assertion ? "rounded-md border-s-4 border-primary bg-primary/5 p-2" : "min-w-0"}><dt className="text-xs font-semibold text-muted">{label}</dt><dd className="break-words">{rendered}</dd></div>;
}

type ActionCardProps = { payload: UnknownRecord; expiresAt: string };
export function AppointmentActionCard({ payload, expiresAt }: ActionCardProps) {
  const { t, locale } = useLocalizedText(); const slot = record(payload.slot); const duration = number(slot.durationMinutes) ?? number(payload.durationMinutes);
  return <dl className="grid grid-cols-1 gap-3 sm:grid-cols-2"><Field label={t("clinic.patientAgent.fields.date")} value={text(slot.date) ? formatDate(text(slot.date)!, locale) : null} /><Field label={t("clinic.patientAgent.fields.startTime")} value={text(slot.startTime)} /><Field label={t("clinic.patientAgent.fields.duration")} value={duration == null ? text(slot.duration) : formatNumber(duration, locale)} /><Field label={t("clinic.patientAgent.fields.appointmentType")} value={text(payload.appointmentType)} /><Field label={t("clinic.patientAgent.fields.service")} value={text(slot.service) ?? text(payload.service)} /><Field label={t("clinic.patientAgent.fields.practitioner")} value={text(slot.practitioner) ?? text(payload.practitioner)} /><Field label={t("clinic.patientAgent.fields.description")} value={text(payload.description)} /><Field label={t("clinic.patientAgent.fields.warnings")} value={list(payload.availabilityWarnings)} /><Field label={t("clinic.patientAgent.fields.expiration")} value={formatDateTime(expiresAt, locale)} /></dl>;
}

export function PrescriptionActionCard({ payload, expiresAt }: ActionCardProps) {
  const { t, locale } = useLocalizedText(); const items = list(payload.items).map(record);
  return <div className="space-y-3"><dl className="grid grid-cols-1 gap-3 sm:grid-cols-2"><Field label={t("clinic.patientAgent.fields.template")} value={text(payload.templateName) ?? text(payload.templateId)} /><Field label={t("clinic.patientAgent.fields.templateVersion")} value={text(payload.templateVersion)} /><Field label={t("clinic.patientAgent.fields.coverage")} value={text(payload.insuranceCoverage)} /><Field label={t("clinic.patientAgent.fields.warnings")} value={list(payload.warnings)} /><Field label={t("clinic.patientAgent.fields.expiration")} value={formatDateTime(expiresAt, locale)} /></dl>{items.map((item, index) => <dl key={index} className="grid grid-cols-1 gap-2 rounded-lg border border-border p-3 sm:grid-cols-2"><Field label={t("clinic.patientAgent.fields.medication")} value={text(item.displayName) ?? text(item.referenceCode)} /><Field label={t("clinic.patientAgent.fields.dose")} value={text(item.dose) ?? text(record(item.dosage).dose) ?? text(item.dosageJson)} /><Field label={t("clinic.patientAgent.fields.frequency")} value={text(item.frequency) ?? text(record(item.dosage).frequency)} /><Field label={t("clinic.patientAgent.fields.route")} value={text(item.route) ?? text(record(item.dosage).route)} /><Field label={t("clinic.patientAgent.fields.duration")} value={number(item.durationDays)} /><Field label={t("clinic.patientAgent.fields.instructions")} value={text(item.instructions) ?? text(record(item.dosage).instructions)} /></dl>)}</div>;
}

export function ClinicalEventActionCard({ payload, expiresAt }: ActionCardProps) {
  const { t, locale } = useLocalizedText(); const content = record(payload.content); const performed = text(content.performedAt);
  return <dl className="grid grid-cols-1 gap-3 sm:grid-cols-2"><Field label={t("clinic.patientAgent.fields.procedure")} value={text(payload.procedureName) ?? text(content.procedureId)} /><Field label={t("clinic.patientAgent.fields.visitNumber")} value={number(content.visitNumber)} /><Field label={t("clinic.patientAgent.fields.eventType")} value={text(content.eventType)} /><Field label={t("clinic.patientAgent.fields.completionStatus")} value={text(content.completionStatus)} /><Field label={t("clinic.patientAgent.fields.performedAt")} value={performed ? formatDateTime(performed, locale) : null} /><Field assertion label={t("clinic.patientAgent.fields.observations")} value={text(content.observations)} /><Field assertion label={t("clinic.patientAgent.fields.diagnosis")} value={text(content.diagnosis)} /><Field assertion label={t("clinic.patientAgent.fields.plan")} value={text(content.planOrFollowUp)} /><Field label={t("clinic.patientAgent.fields.narrative")} value={text(content.narrativeNote)} /><Field label={t("clinic.patientAgent.fields.attachments")} value={list(content.attachmentReferences)} /><Field label={t("clinic.patientAgent.fields.warnings")} value={list(payload.warnings)} /><Field label={t("clinic.patientAgent.fields.expiration")} value={formatDateTime(expiresAt, locale)} /></dl>;
}

export function PatientAgentActionReview({ action, patientId }: { action: PatientAgentAction; patientId: string }) {
  const { t } = useLocalizedText(); const confirm = useConfirmPatientAgentAction(patientId); const reject = useRejectPatientAgentAction(patientId); const [local, setLocal] = useState(action); const pending = confirm.isPending || reject.isPending;
  useEffect(() => setLocal(action), [action]);
  const payload = record(local.reviewPayload); const completion = record(local.completionPayload); const completionId = text(completion.appointmentId) ?? text(completion.prescriptionId) ?? text(completion.eventId) ?? text(completion.trackingCode);
  const confirmLabel = local.actionType.includes("appointment") ? t("clinic.patientAgent.actions.confirmAppointment") : local.actionType.includes("prescription") ? t("clinic.patientAgent.actions.confirmPrescription") : t("clinic.patientAgent.actions.confirmClinicalEvent");
  const card = local.actionType.includes("appointment") ? <AppointmentActionCard payload={payload} expiresAt={local.expiresAt} /> : local.actionType.includes("prescription") ? <PrescriptionActionCard payload={payload} expiresAt={local.expiresAt} /> : <ClinicalEventActionCard payload={payload} expiresAt={local.expiresAt} />;
  const act = async (kind: "confirm" | "reject") => { try { const result = kind === "confirm" ? await confirm.mutateAsync(local.actionId) : await reject.mutateAsync(local.actionId); setLocal((current) => ({ ...current, ...result })); } catch { setLocal((current) => ({ ...current, status: "Failed", failure: { code: "request_failed", message: t("clinic.patientAgent.actionError"), retryable: true } })); } };
  return <Card><Card.Header className="flex justify-between gap-3"><Card.Title>{local.capability}</Card.Title><PatientAgentActionStatus status={pending ? "Confirming" : local.status} completionId={completionId} failure={local.failure?.message} /></Card.Header><Card.Content className="space-y-4">{card}{local.status === "Proposed" ? <div className="flex flex-wrap gap-2"><Tooltip><Button variant="primary" isDisabled={pending} onPress={() => void act("confirm")}><FaIcon icon="check" aria-hidden="true" />{confirmLabel}</Button><Tooltip.Content>{confirmLabel}</Tooltip.Content></Tooltip><Tooltip><Button variant="outline" isDisabled={pending} onPress={() => void act("reject")}><FaIcon icon="xmark" aria-hidden="true" />{t("clinic.patientAgent.actions.reject")}</Button><Tooltip.Content>{t("clinic.patientAgent.actions.reject")}</Tooltip.Content></Tooltip></div> : null}</Card.Content></Card>;
}

function PatientAgentActionLoader({ patientId, actionId }: { patientId: string; actionId: string }) { const query = usePatientAgentAction(patientId, actionId); if (query.isLoading) return <PatientAgentActivity />; if (query.error || !query.data) return <PatientAgentErrorState onRetry={() => void query.refetch()} />; return <PatientAgentActionReview patientId={patientId} action={query.data} />; }
export function PatientAgentActionList({ patientId, actionIds }: { patientId: string; actionIds: string[] }) { return <div className="flex flex-col gap-3">{actionIds.map((id) => <PatientAgentActionLoader key={id} patientId={patientId} actionId={id} />)}</div>; }

export function PatientAgentPanel({ patientId, patientName }: { patientId: string; patientName: string }) {
  const { t, locale } = useLocalizedText(); const conversation = usePatientAgentConversation(patientId); const [conversationId, setConversationId] = useState<string | null>(null); const [messages, setMessages] = useState<PatientAgentMessageResult[]>([]); const [actionIds, setActionIds] = useState<string[]>([]); const [lastMessage, setLastMessage] = useState(""); const focusTarget = useRef<HTMLDivElement>(null); const activePatient = useRef(patientId); const owner = useId();
  useEffect(() => { activePatient.current = patientId; setConversationId(null); setMessages([]); setActionIds([]); conversation.reset(); }, [patientId]);
  useEffect(() => { if (messages.length || actionIds.length) focusTarget.current?.focus(); }, [messages.length, actionIds.length]);
  const submit = async (message: string) => { const submittedPatient = patientId; setLastMessage(message); const result = await conversation.submit(message, conversationId); if (activePatient.current !== submittedPatient) return; setConversationId(result.conversationId); setMessages((current) => [...current, ...result.messages]); setActionIds((current) => [...current, ...result.actions.map((action) => action.id).filter((id) => !current.includes(id))]); };
  return <section key={`${owner}-${patientId}`} dir={locale === "fa" ? "rtl" : "ltr"} className="space-y-4" aria-label={t("clinic.patientAgent.title")}><PatientAgentHeader patientName={patientName} /><div ref={focusTarget} tabIndex={-1} className="space-y-3 outline-none"><PatientAgentMessageList messages={messages} />{conversation.isPending ? <PatientAgentActivity /> : null}{conversation.isError ? <PatientAgentErrorState onRetry={lastMessage ? () => void submit(lastMessage) : undefined} /> : null}<PatientAgentActionList patientId={patientId} actionIds={actionIds} /></div><PatientAgentComposer disabled={conversation.isPending} onSubmit={submit} /></section>;
}

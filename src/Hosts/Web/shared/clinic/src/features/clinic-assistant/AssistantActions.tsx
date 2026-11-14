"use client";

import { Button, Card, Tooltip } from "@heroui/react";
import { useQueries } from "@tanstack/react-query";
import { ActionReviewGroup, type ActionReviewItem, type ActionReviewItemState } from "@vitarish/intelligence";
import { FaIcon } from "@vitarish/ui";
import { useState } from "react";
import { clinicGetAssistantAction } from "../../api/generated/clinic/clinic";
import { ClinicProfileActionCard, ManageActionCard, StaffInviteActionCard, StaffRemovalActionCard } from "./ManageActionCard";
import { operationsActionTypes } from "./operationsActionTypes";
import { useLocalizedText } from "../../localization";
import {
  AppointmentActionCard,
  ClinicalEventActionCard,
  OperationsActionCard,
  PrescriptionActionCard,
  SurgeryActionCard,
} from "./ActionCards";
import { asRecord, asText } from "./actionPayload";
import { isSurgeryActionType } from "./surgeryActionTypes";
import type { AssistantAction } from "./hooks";
import {
  useConfirmAssistantAction,
  useConfirmOperationsAssistantAction,
  useOperationsAssistantAction,
  useAssistantAction,
  useRejectOperationsAssistantAction,
  useRejectAssistantAction,
  useReviseOperationsAssistantAction,
  operationsAssistantActionQueryKey,
} from "./hooks";
import {
  AssistantActivity,
  AssistantErrorState,
} from "./AssistantStates";

export function AssistantActionStatus({
  status,
  completionId,
  failure,
}: {
  status: AssistantAction["status"];
  completionId?: string | null;
  failure?: string | null;
}) {
  const { t } = useLocalizedText();
  const icon =
    status === "Completed"
      ? "circle-check"
      : status === "Failed"
        ? "triangle-exclamation"
        : "circle-info";
  return (
    <div role="status" aria-live="polite" className="text-sm font-medium">
      <FaIcon icon={icon} className="me-2" aria-hidden="true" />
      {t(`clinic.clinicAssistant.status.${status.toLowerCase()}`)}
      {completionId ? (
        <span className="ms-2 font-normal">
          {t("clinic.clinicAssistant.completionId", { id: completionId })}
        </span>
      ) : null}
      {failure ? (
        <p className="mt-1 font-normal text-danger">{failure}</p>
      ) : null}
    </div>
  );
}

export function AssistantActionReview({
  action,
  patientId,
}: {
  action: AssistantAction;
  patientId: string;
}) {
  const { t } = useLocalizedText();
  const confirm = useConfirmAssistantAction(patientId);
  const reject = useRejectAssistantAction(patientId);
  const [requestError, setRequestError] = useState(false);
  const pending = confirm.isPending || reject.isPending;
  const payload = asRecord(action.reviewPayload);
  const completion = asRecord(action.completionPayload);
  const completionId =
    asText(completion.appointmentId) ??
    asText(completion.prescriptionId) ??
    asText(completion.eventId) ??
    asText(completion.trackingCode) ??
    asText(completion.entityId);
  const isAppointment = action.actionType === "create_appointment";
  const isPrescription = action.actionType === "create_prescription";
  const isClinicalEvent = action.actionType === "add_clinical_event";
  const surgeryActionType = isSurgeryActionType(action.actionType) ? action.actionType : null;
  const confirmLabel = surgeryActionType
    ? t("clinic.surgeries.assistant.confirm")
    : isAppointment
    ? t("clinic.clinicAssistant.actions.confirmAppointment")
    : isPrescription
      ? t("clinic.clinicAssistant.actions.confirmPrescription")
      : isClinicalEvent
        ? t("clinic.clinicAssistant.actions.confirmClinicalEvent")
        : null;
  const review = surgeryActionType ? (
    <SurgeryActionCard actionType={surgeryActionType} payload={payload} expiresAt={action.expiresAt} />
  ) : isAppointment ? (
    <AppointmentActionCard payload={payload} expiresAt={action.expiresAt} />
  ) : isPrescription ? (
    <PrescriptionActionCard payload={payload} expiresAt={action.expiresAt} />
  ) : isClinicalEvent ? (
    <ClinicalEventActionCard payload={payload} expiresAt={action.expiresAt} />
  ) : null;

  const update = async (kind: "confirm" | "reject") => {
    setRequestError(false);
    try {
      await (kind === "confirm"
        ? confirm.mutateAsync(action.actionId)
        : reject.mutateAsync(action.actionId));
    } catch {
      // A transport failure does not change the authoritative business status.
      setRequestError(true);
    }
  };

  return (
    <Card>
      <Card.Header className="flex justify-between gap-3">
        <Card.Title>{action.capability}</Card.Title>
        <AssistantActionStatus
          status={pending ? "Confirming" : action.status}
          completionId={completionId}
          failure={action.failure?.message}
        />
      </Card.Header>
      <Card.Content className="space-y-4">
        {review ?? (
          <p role="alert" className="text-sm text-danger">
            {t("clinic.clinicAssistant.actionUnavailable")}
          </p>
        )}
        {requestError ? (
          <p role="alert" className="text-sm text-danger">
            {t("clinic.clinicAssistant.actionError")}
          </p>
        ) : null}
        {action.status === "Proposed" && confirmLabel ? (
          <div className="flex flex-wrap gap-2">
            <Tooltip>
              <Button
                variant="primary"
                isDisabled={pending}
                onPress={() => void update("confirm")}
              >
                <FaIcon icon="check" aria-hidden="true" />
                {confirmLabel}
              </Button>
              <Tooltip.Content>{confirmLabel}</Tooltip.Content>
            </Tooltip>
            <Tooltip>
              <Button
                variant="outline"
                isDisabled={pending}
                onPress={() => void update("reject")}
              >
                <FaIcon icon="xmark" aria-hidden="true" />
                {t("clinic.clinicAssistant.actions.reject")}
              </Button>
              <Tooltip.Content>
                {t("clinic.clinicAssistant.actions.reject")}
              </Tooltip.Content>
            </Tooltip>
          </div>
        ) : null}
      </Card.Content>
    </Card>
  );
}

function AssistantActionLoader({
  patientId,
  actionId,
}: {
  patientId: string;
  actionId: string;
}) {
  const action = useAssistantAction(patientId, actionId);
  if (action.isLoading) return <AssistantActivity />;
  if (action.error || !action.data)
    return <AssistantErrorState onRetry={() => void action.refetch()} />;
  return (
    <AssistantActionReview patientId={patientId} action={action.data} />
  );
}

export function AssistantActionList({
  patientId,
  actionIds,
}: {
  patientId: string;
  actionIds: string[];
}) {
  return (
    <div className="flex flex-col gap-3">
      {actionIds.map((actionId) => (
        <AssistantActionLoader
          key={actionId}
          patientId={patientId}
          actionId={actionId}
        />
      ))}
    </div>
  );
}

export function OperationsAssistantActionReview({
  action,
  onReplaced,
}: {
  action: AssistantAction;
  /** Called when the owner's edit replaced this proposal with a new one. */
  onReplaced?: (originalId: string, replacementId: string) => void;
}) {
  const { t } = useLocalizedText();
  const confirm = useConfirmOperationsAssistantAction();
  const reject = useRejectOperationsAssistantAction();
  const revise = useReviseOperationsAssistantAction();
  const [requestError, setRequestError] = useState(false);
  const pending = confirm.isPending || reject.isPending;
  const payload = asRecord(action.reviewPayload);
  const completion = asRecord(action.completionPayload);
  const completionId = asText(completion.entityId) ?? asText(completion.operationId);
  const update = async (kind: "confirm" | "reject") => {
    setRequestError(false);
    try {
      await (kind === "confirm" ? confirm.mutateAsync(action.actionId) : reject.mutateAsync(action.actionId));
    } catch {
      setRequestError(true);
    }
  };
  return (
    <Card>
      <Card.Header className="flex justify-between gap-3">
        <Card.Title>{action.actionType === operationsActionTypes.inviteMember ? t("clinic.operationsAssistant.invite.title")
          : action.actionType === operationsActionTypes.removeMember ? t("clinic.operationsAssistant.removeMember.title")
          : action.actionType === operationsActionTypes.updateClinicProfile ? t("clinic.operationsAssistant.clinicProfile.title")
          : t("clinic.operationsAssistant.reviewTitle")}</Card.Title>
        <AssistantActionStatus status={pending ? "Confirming" : action.status} completionId={completionId} failure={action.failure?.message} />
      </Card.Header>
      <Card.Content className="space-y-4">
        {isSurgeryActionType(action.actionType) ? (
          <SurgeryActionCard actionType={action.actionType} payload={payload} expiresAt={action.expiresAt} />
        ) : action.actionType === operationsActionTypes.appointmentManage ? (
          <ManageActionCard
            reviewPayload={action.reviewPayload}
            onRevise={action.status === "Proposed" && !pending ? async (argumentsJson) => {
              const replacement = await revise.mutateAsync({ actionId: action.actionId, argumentsJson });
              onReplaced?.(action.actionId, replacement.actionId);
            } : undefined}
          />
        ) : action.actionType === operationsActionTypes.inviteMember ? (
          <StaffInviteActionCard
            reviewPayload={action.reviewPayload}
            onRevise={action.status === "Proposed" && !pending ? async (argumentsJson) => {
              const replacement = await revise.mutateAsync({ actionId: action.actionId, argumentsJson });
              onReplaced?.(action.actionId, replacement.actionId);
            } : undefined}
          />
        ) : action.actionType === operationsActionTypes.removeMember ? (
          <StaffRemovalActionCard reviewPayload={action.reviewPayload} />
        ) : action.actionType === operationsActionTypes.updateClinicProfile ? (
          <ClinicProfileActionCard reviewPayload={action.reviewPayload} />
        ) : (
          <OperationsActionCard payload={payload} expiresAt={action.expiresAt} />
        )}
        {requestError ? <p role="alert" className="text-sm text-danger">{t("clinic.operationsAssistant.actionError")}</p> : null}
        {action.status === "Proposed" ? (
          <div className="flex flex-wrap gap-2">
            <Tooltip><Button variant="primary" isDisabled={pending} onPress={() => void update("confirm")}><FaIcon icon="check" aria-hidden="true" />{t("clinic.operationsAssistant.confirm")}</Button><Tooltip.Content>{t("clinic.operationsAssistant.confirm")}</Tooltip.Content></Tooltip>
            <Tooltip><Button variant="outline" isDisabled={pending} onPress={() => void update("reject")}><FaIcon icon="xmark" aria-hidden="true" />{t("clinic.operationsAssistant.reject")}</Button><Tooltip.Content>{t("clinic.operationsAssistant.reject")}</Tooltip.Content></Tooltip>
          </div>
        ) : null}
      </Card.Content>
    </Card>
  );
}

function OperationsAssistantActionLoader({ actionId }: { actionId: string }) {
  const action = useOperationsAssistantAction(actionId);
  if (action.isLoading) return <AssistantActivity />;
  if (action.error || !action.data) return <AssistantErrorState onRetry={() => void action.refetch()} />;
  return <OperationsAssistantActionReview action={action.data} />;
}

export function OperationsAssistantActionList({ actionIds }: { actionIds: string[] }) {
  return <div className="flex flex-col gap-3">{actionIds.map((actionId) => <OperationsAssistantActionLoader key={actionId} actionId={actionId} />)}</div>;
}

function reviewState(action: AssistantAction): ActionReviewItemState {
  switch (action.status) {
    case "Proposed": return "pending";
    case "Completed": return "done";
    case "Failed": return "failed";
    default: return "closed";
  }
}

/** Everything one operations reply prepared, reviewed together and confirmed one by one (ADR-0022). */
export function OperationsActionGroup({
  actionIds,
  onReplaced,
}: {
  actionIds: readonly string[];
  onReplaced?: (originalId: string, replacementId: string) => void;
}) {
  const confirm = useConfirmOperationsAssistantAction();
  const queries = useQueries({
    queries: actionIds.map((actionId) => ({
      queryKey: operationsAssistantActionQueryKey(actionId),
      queryFn: ({ signal }: { signal: AbortSignal }) => clinicGetAssistantAction(actionId, undefined, signal),
      staleTime: 0,
    })),
  });
  if (queries.some((query) => query.isLoading)) return <AssistantActivity />;
  const failed = queries.find((query) => query.error || !query.data);
  if (failed) return <AssistantErrorState onRetry={() => void failed.refetch()} />;
  const items: ActionReviewItem[] = queries.map((query) => {
    const action = query.data!;
    return {
      id: action.actionId,
      state: reviewState(action),
      retryable: action.failure?.retryable === true,
      content: <OperationsAssistantActionReview action={action} onReplaced={onReplaced} />,
    };
  });
  return <ActionReviewGroup items={items} confirm={async (id) => { await confirm.mutateAsync(id); }} />;
}

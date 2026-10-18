"use client";

import { Button, Card, Tooltip } from "@heroui/react";
import { FaIcon } from "@vitarish/ui";
import { useState } from "react";
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

export function OperationsAssistantActionReview({ action }: { action: AssistantAction }) {
  const { t } = useLocalizedText();
  const confirm = useConfirmOperationsAssistantAction();
  const reject = useRejectOperationsAssistantAction();
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
        <Card.Title>{t("clinic.operationsAssistant.reviewTitle")}</Card.Title>
        <AssistantActionStatus status={pending ? "Confirming" : action.status} completionId={completionId} failure={action.failure?.message} />
      </Card.Header>
      <Card.Content className="space-y-4">
        {isSurgeryActionType(action.actionType) ? (
          <SurgeryActionCard actionType={action.actionType} payload={payload} expiresAt={action.expiresAt} />
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

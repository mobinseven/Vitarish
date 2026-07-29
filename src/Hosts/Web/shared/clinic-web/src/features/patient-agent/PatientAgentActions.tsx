"use client";

import { Button, Card, Tooltip } from "@heroui/react";
import { FaIcon } from "@vitarish/web-ui";
import { useState } from "react";
import { useLocalizedText } from "../../localization";
import {
  AppointmentActionCard,
  ClinicalEventActionCard,
  PrescriptionActionCard,
} from "./ActionCards";
import { asRecord, asText } from "./actionPayload";
import type { PatientAgentAction } from "./hooks";
import {
  useConfirmPatientAgentAction,
  usePatientAgentAction,
  useRejectPatientAgentAction,
} from "./hooks";
import {
  PatientAgentActivity,
  PatientAgentErrorState,
} from "./PatientAgentStates";

export function PatientAgentActionStatus({
  status,
  completionId,
  failure,
}: {
  status: PatientAgentAction["status"];
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
      {t(`clinic.patientAgent.status.${status.toLowerCase()}`)}
      {completionId ? (
        <span className="ms-2 font-normal">
          {t("clinic.patientAgent.completionId", { id: completionId })}
        </span>
      ) : null}
      {failure ? (
        <p className="mt-1 font-normal text-danger">{failure}</p>
      ) : null}
    </div>
  );
}

export function PatientAgentActionReview({
  action,
  patientId,
}: {
  action: PatientAgentAction;
  patientId: string;
}) {
  const { t } = useLocalizedText();
  const confirm = useConfirmPatientAgentAction(patientId);
  const reject = useRejectPatientAgentAction(patientId);
  const [requestError, setRequestError] = useState(false);
  const pending = confirm.isPending || reject.isPending;
  const payload = asRecord(action.reviewPayload);
  const completion = asRecord(action.completionPayload);
  const completionId =
    asText(completion.appointmentId) ??
    asText(completion.prescriptionId) ??
    asText(completion.eventId) ??
    asText(completion.trackingCode);
  const isAppointment = action.actionType === "create_appointment";
  const isPrescription = action.actionType === "create_prescription";
  const isClinicalEvent = action.actionType === "add_clinical_event";
  const confirmLabel = isAppointment
    ? t("clinic.patientAgent.actions.confirmAppointment")
    : isPrescription
      ? t("clinic.patientAgent.actions.confirmPrescription")
      : isClinicalEvent
        ? t("clinic.patientAgent.actions.confirmClinicalEvent")
        : null;
  const review = isAppointment ? (
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
        <PatientAgentActionStatus
          status={pending ? "Confirming" : action.status}
          completionId={completionId}
          failure={action.failure?.message}
        />
      </Card.Header>
      <Card.Content className="space-y-4">
        {review ?? (
          <p role="alert" className="text-sm text-danger">
            {t("clinic.patientAgent.actionUnavailable")}
          </p>
        )}
        {requestError ? (
          <p role="alert" className="text-sm text-danger">
            {t("clinic.patientAgent.actionError")}
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
                {t("clinic.patientAgent.actions.reject")}
              </Button>
              <Tooltip.Content>
                {t("clinic.patientAgent.actions.reject")}
              </Tooltip.Content>
            </Tooltip>
          </div>
        ) : null}
      </Card.Content>
    </Card>
  );
}

function PatientAgentActionLoader({
  patientId,
  actionId,
}: {
  patientId: string;
  actionId: string;
}) {
  const action = usePatientAgentAction(patientId, actionId);
  if (action.isLoading) return <PatientAgentActivity />;
  if (action.error || !action.data)
    return <PatientAgentErrorState onRetry={() => void action.refetch()} />;
  return (
    <PatientAgentActionReview patientId={patientId} action={action.data} />
  );
}

export function PatientAgentActionList({
  patientId,
  actionIds,
}: {
  patientId: string;
  actionIds: string[];
}) {
  return (
    <div className="flex flex-col gap-3">
      {actionIds.map((actionId) => (
        <PatientAgentActionLoader
          key={actionId}
          patientId={patientId}
          actionId={actionId}
        />
      ))}
    </div>
  );
}

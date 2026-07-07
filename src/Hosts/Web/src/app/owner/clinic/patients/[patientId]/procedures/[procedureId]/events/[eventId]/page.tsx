"use client";

import { use, useState } from "react";
import { useRouter } from "next/navigation";
import { Button, Chip } from "@heroui/react";
import { Widget } from "@heroui-pro/react";
import { Routes } from "@/app/routes";
import { ClinicDetailGrid, type ClinicDetailItem } from "@/features/clinic/components/ClinicDetailGrid";
import { ClinicErrorState, ClinicLoadingState } from "@/features/clinic/components/ClinicStateSurface";
import { usePatientProcedureEventDetail } from "@/features/clinic/patients/procedures/hooks/usePatientProcedureEventDetail";
import { useCommitPatientProcedureEvent, useUncommitPatientProcedureEvent } from "@/features/clinic/patients/procedures/hooks/usePatientProcedureEventMutations";
import { PatientProcedureEventForm } from "@/features/clinic/patients/procedures/components/PatientProcedureEventForm";
import { PatientProcedureEventDeleteDialog } from "@/features/clinic/patients/procedures/components/PatientProcedureEventDeleteDialog";
import { PatientProcedureEventItemsPanel } from "@/features/clinic/patients/procedures/components/PatientProcedureEventItemsPanel";
import { PatientProcedureEventAccessPanel } from "@/features/clinic/patients/procedures/components/PatientProcedureEventAccessPanel";
import { useLocalizedText, formatDateTime } from "@/localization";

export default function ClinicPatientProcedureEventPage({
  params,
}: {
  params: Promise<{ patientId: string; procedureId: string; eventId: string }>;
}) {
  const { patientId, procedureId, eventId } = use(params);
  const router = useRouter();
  const { t, locale } = useLocalizedText();
  const { data: event, isLoading, error } = usePatientProcedureEventDetail(eventId);

  const commitMutation = useCommitPatientProcedureEvent();
  const uncommitMutation = useUncommitPatientProcedureEvent();

  const [showEditForm, setShowEditForm] = useState(false);
  const [showDeleteDialog, setShowDeleteDialog] = useState(false);

  if (isLoading) {
    return <ClinicLoadingState label={t("common.states.loading")} />;
  }

  if (error || !event) {
    return <ClinicErrorState title={t("clinic.procedures.eventNotFound")} />;
  }

  const details: ClinicDetailItem[] = [
    { label: t("clinic.fields.created"), value: formatDateTime(event.createdAt, locale) },
    ...(event.committedAt
      ? [{ label: t("clinic.fields.committedAt"), value: formatDateTime(event.committedAt, locale) }]
      : []),
    ...(event.description
      ? [{ label: t("clinic.fields.description"), value: event.description }]
      : []),
  ];

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Button variant="ghost" className="mb-2" onPress={() => router.push(Routes.Clinic.Patients.Procedure(patientId, procedureId))}>
          <i aria-hidden className="fa-solid fa-arrow-left" />
          {t("common.actions.previous")}
        </Button>
        <div className="flex items-start justify-between">
          <div>
            <h1 className="text-2xl font-bold">{event.title}</h1>
            <div className="flex items-center gap-2 mt-1">
              <Chip size="sm" color={event.isCommitted ? "success" : "default"} variant="soft">
                {event.isCommitted ? t("clinic.procedures.committed") : t("clinic.procedures.draft")}
              </Chip>
              <span className="text-xs text-muted">
                Step {event.position + 1}
              </span>
            </div>
          </div>
          <div className="flex gap-2">
            <Button size="sm" variant="secondary" onPress={() => setShowEditForm(true)}>
              {t("common.actions.edit")}
            </Button>
            <Button
              size="sm"
              variant={event.isCommitted ? "secondary" : "primary"}
              isDisabled={commitMutation.isPending || uncommitMutation.isPending}
              onPress={() =>
                event.isCommitted
                  ? uncommitMutation.mutate(event.id)
                  : commitMutation.mutate(event.id)
              }
            >
              {event.isCommitted ? t("clinic.procedures.events.reopen") : t("clinic.procedures.events.commit")}
            </Button>
            <Button size="sm" variant="danger" onPress={() => setShowDeleteDialog(true)}>
              {t("common.actions.delete")}
            </Button>
          </div>
        </div>
      </div>

      <Widget>
        <Widget.Content>
          <ClinicDetailGrid items={details} />
        </Widget.Content>
      </Widget>

      <PatientProcedureEventItemsPanel eventId={eventId} />

      <PatientProcedureEventAccessPanel eventId={eventId} />

      {showEditForm && (
        <PatientProcedureEventForm
          procedureId={procedureId}
          projectId={event.projectId}
          eventId={event.id}
          initialTitle={event.title}
          initialDescription={event.description}
          onClose={() => setShowEditForm(false)}
        />
      )}

      {showDeleteDialog && (
        <PatientProcedureEventDeleteDialog
          eventId={event.id}
          eventTitle={event.title}
          onClose={() => setShowDeleteDialog(false)}
          onDeleted={() => router.push(Routes.Clinic.Patients.Procedure(patientId, procedureId))}
        />
      )}
    </div>
  );
}

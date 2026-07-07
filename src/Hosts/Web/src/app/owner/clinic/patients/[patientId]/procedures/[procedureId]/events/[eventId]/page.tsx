"use client";

import { use } from "react";
import { useRouter } from "next/navigation";
import { Button } from "@heroui/react";
import { Widget } from "@heroui-pro/react";
import { Routes } from "@/app/routes";
import { ClinicDetailGrid, type ClinicDetailItem } from "@/features/clinic/components/ClinicDetailGrid";
import { ClinicErrorState, ClinicLoadingState } from "@/features/clinic/components/ClinicStateSurface";
import { useProcedureEventDetail } from "@/features/clinic/patients/procedures/hooks/useProcedures";
import { useLocalizedText, formatDateTime } from "@/localization";

export default function ClinicPatientProcedureEventPage({
  params,
}: {
  params: Promise<{ patientId: string; procedureId: string; eventId: string }>;
}) {
  const { patientId, procedureId, eventId } = use(params);
  const router = useRouter();
  const { t, locale } = useLocalizedText();
  const { data: event, isLoading, error } = useProcedureEventDetail(eventId);

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
        <h1 className="text-2xl font-bold">{event.title}</h1>
        <span className={`text-xs ${event.isCommitted ? "text-success" : "text-muted"}`}>
          {event.isCommitted ? t("clinic.procedures.committed") : t("clinic.procedures.draft")}
        </span>
      </div>

      <Widget>
        <Widget.Content>
          <ClinicDetailGrid items={details} />
        </Widget.Content>
      </Widget>
    </div>
  );
}

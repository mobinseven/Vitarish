"use client";

import { use } from "react";
import { useRouter } from "next/navigation";
import { Button } from "@heroui/react";
import { ListView, Widget } from "@heroui-pro/react";
import { Routes } from "@/app/routes";
import { ClinicEmptyState, ClinicErrorState, ClinicLoadingState } from "@/features/clinic/components/ClinicStateSurface";
import { usePatientProcedureDetail, usePatientProcedureEvents } from "@/features/clinic/patients/procedures/hooks/useProcedures";
import { useLocalizedText, formatDate } from "@/localization";

export default function ClinicPatientProcedurePage({
  params,
}: {
  params: Promise<{ patientId: string; procedureId: string }>;
}) {
  const { patientId, procedureId } = use(params);
  const router = useRouter();
  const { t, locale } = useLocalizedText();
  const { data: procedure, isLoading, error } = usePatientProcedureDetail(procedureId);
  const { data: events } = usePatientProcedureEvents(procedureId);

  if (isLoading) {
    return <ClinicLoadingState label={t("common.states.loading")} />;
  }

  if (error || !procedure) {
    return <ClinicErrorState title={t("clinic.procedures.notFound")} />;
  }

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Button variant="ghost" className="mb-2" onPress={() => router.push(Routes.Clinic.Patients.Detail(patientId))}>
          <i aria-hidden className="fa-solid fa-arrow-left" />
          {t("clinic.patients.backToPatients")}
        </Button>
        <h1 className="text-2xl font-bold">{procedure.name}</h1>
        {procedure.description && (
          <p className="text-sm text-muted">{procedure.description}</p>
        )}
      </div>

      <Widget>
        <Widget.Header>
          <Widget.Title>{t("clinic.procedures.events")}</Widget.Title>
        </Widget.Header>
        <Widget.Content>
          <ListView
            aria-label={t("clinic.procedures.events")}
            onAction={(key) => router.push(Routes.Clinic.Patients.ProcedureEvent(patientId, procedureId, String(key)))}
            renderEmptyState={() => <ClinicEmptyState title={t("clinic.procedures.eventsEmpty")} />}
            variant="secondary"
          >
            {events?.map((event) => (
              <ListView.Item key={event.id} id={event.id} textValue={event.title}>
                <ListView.ItemContent>
                  <div className="flex flex-col gap-1">
                    <ListView.Title>{event.title}</ListView.Title>
                    <ListView.Description>{formatDate(event.createdAt, locale)}</ListView.Description>
                  </div>
                </ListView.ItemContent>
                <ListView.ItemAction>
                  <span className={`text-xs ${event.isCommitted ? "text-success" : "text-muted"}`}>
                    {event.isCommitted ? t("clinic.procedures.committed") : t("clinic.procedures.draft")}
                  </span>
                </ListView.ItemAction>
              </ListView.Item>
            ))}
          </ListView>
        </Widget.Content>
      </Widget>
    </div>
  );
}

"use client";

import { use, useCallback } from "react";
import { useRouter } from "next/navigation";
import { Button } from "@heroui/react";
import { Widget } from "@heroui-pro/react";
import { Routes } from "@/app/routes";
import { ClinicErrorState, ClinicLoadingState } from "@vitarish/clinic-web";
import { usePatientProcedureDetail } from "@vitarish/clinic-web";
import { usePatientProcedureEvents } from "@vitarish/clinic-web";
import { PatientProcedureHeader } from "@vitarish/clinic-web";
import { PatientProcedureEventTimeline } from "@vitarish/clinic-web";
import { useLocalizedText } from "@/localization";

export default function ClinicPatientProcedurePage({
  params,
}: {
  params: Promise<{ patientId: string; procedureId: string }>;
}) {
  const { patientId, procedureId } = use(params);
  const router = useRouter();
  const { t } = useLocalizedText();
  const { data: procedure, isLoading, error } = usePatientProcedureDetail(procedureId);
  const { data: events } = usePatientProcedureEvents(procedureId);

  const handleDeleted = useCallback(() => {
    router.push(Routes.Clinic.Patients.Detail(patientId));
  }, [patientId, router]);

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
      </div>

      <PatientProcedureHeader
        procedure={procedure}
        patientId={patientId}
        onDeleted={handleDeleted}
      />

      <Widget>
        <Widget.Header>
          <Widget.Title>{t("clinic.procedures.events.title")}</Widget.Title>
        </Widget.Header>
        <Widget.Content>
          <PatientProcedureEventTimeline
            procedureId={procedureId}
            patientId={patientId}
            events={events ?? []}
          />
        </Widget.Content>
      </Widget>
    </div>
  );
}

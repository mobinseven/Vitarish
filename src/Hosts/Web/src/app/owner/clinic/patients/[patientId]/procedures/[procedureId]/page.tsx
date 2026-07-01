"use client";

import { use } from "react";
import { useRouter } from "next/navigation";
import { Card, Button, Spinner } from "@heroui/react";
import { Routes } from "@/app/routes";
import { usePatientProcedureDetail, usePatientProcedureEvents } from "@/features/clinic/patients/procedures/hooks/useProcedures";
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

  if (isLoading) {
    return (
      <Card className="p-8">
        <div className="flex items-center justify-center gap-2">
          <Spinner size="sm" />
          <span className="text-default-500">{t("common.loading")}</span>
        </div>
      </Card>
    );
  }

  if (error || !procedure) {
    return (
      <Card className="p-8">
        <p className="text-danger">{error?.message ?? t("clinic.procedures.notFound")}</p>
      </Card>
    );
  }

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Button variant="ghost" className="mb-2" onPress={() => router.push(Routes.Clinic.Patients.Detail(patientId))}>
          ← {t("clinic.patients.backToPatients")}
        </Button>
        <h1 className="text-2xl font-bold">{procedure.name}</h1>
        {procedure.description && (
          <p className="text-sm text-default-500">{procedure.description}</p>
        )}
      </div>

      <Card>
        <Card.Header>
          <h2 className="text-lg font-semibold">{t("clinic.procedures.events")}</h2>
        </Card.Header>
        <Card.Content className="flex flex-col gap-1">
          {(!events || events.length === 0) && (
            <p className="py-4 text-center text-sm text-default-500">No events yet.</p>
          )}
          {events?.map((ev) => (
            <button
              key={ev.id}
              onClick={() => router.push(Routes.Clinic.Patients.ProcedureEvent(patientId, procedureId, ev.id))}
              className="flex items-center justify-between rounded-lg px-4 py-3 text-start transition-colors hover:bg-default-100"
            >
              <div className="flex flex-col gap-1">
                <span className="text-sm font-medium">{ev.title}</span>
                <span className="text-xs text-default-500">{new Date(ev.createdAt).toLocaleDateString()}</span>
              </div>
              <span className={`text-xs ${ev.isCommitted ? "text-success" : "text-default-500"}`}>
                {ev.isCommitted ? "Committed" : "Draft"}
              </span>
            </button>
          ))}
        </Card.Content>
      </Card>
    </div>
  );
}

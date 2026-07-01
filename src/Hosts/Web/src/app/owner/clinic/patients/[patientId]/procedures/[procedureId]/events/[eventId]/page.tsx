"use client";

import { use } from "react";
import { useRouter } from "next/navigation";
import { Card, Button, Spinner } from "@heroui/react";
import { Routes } from "@/app/routes";
import { useProcedureEventDetail } from "@/features/clinic/patients/procedures/hooks/useProcedures";
import { useLocalizedText } from "@/localization";

export default function ClinicPatientProcedureEventPage({
  params,
}: {
  params: Promise<{ patientId: string; procedureId: string; eventId: string }>;
}) {
  const { patientId, procedureId, eventId } = use(params);
  const router = useRouter();
  const { t } = useLocalizedText();
  const { data: event, isLoading, error } = useProcedureEventDetail(eventId);

  if (isLoading) {
    return (
      <Card className="p-8">
        <div className="flex items-center justify-center gap-2">
          <Spinner size="sm" />
          <span className="text-default-500">{t("common.states.loading")}</span>
        </div>
      </Card>
    );
  }

  if (error || !event) {
    return (
      <Card className="p-8">
        <p className="text-danger">{error?.message ?? t("clinic.procedures.eventNotFound")}</p>
      </Card>
    );
  }

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Button variant="ghost" className="mb-2" onPress={() => router.push(Routes.Clinic.Patients.Procedure(patientId, procedureId))}>
          ← {t("common.actions.previous")}
        </Button>
        <h1 className="text-2xl font-bold">{event.title}</h1>
        <span className={`text-xs ${event.isCommitted ? "text-success" : "text-default-500"}`}>
          {event.isCommitted ? "Committed" : "Draft"}
        </span>
      </div>

      <Card>
        <Card.Content className="grid gap-4 sm:grid-cols-2">
          <DetailField label="Created" value={new Date(event.createdAt).toLocaleString()} />
          {event.committedAt && (
            <DetailField label="Committed At" value={new Date(event.committedAt).toLocaleString()} />
          )}
          {event.description && (
            <div className="sm:col-span-2">
              <span className="text-xs text-default-500">Description</span>
              <p className="text-sm">{event.description}</p>
            </div>
          )}
        </Card.Content>
      </Card>
    </div>
  );
}

function DetailField({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex flex-col gap-1">
      <span className="text-xs text-default-500">{label}</span>
      <span className="text-sm">{value}</span>
    </div>
  );
}

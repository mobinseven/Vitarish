"use client";

import { useSeaEventCapabilities } from "@vitarish/sea-web";
import { DetailGrid, type DetailGridItem } from "@vitarish/web-ui";
import { formatDateTime, useLocalizedText } from "../../../localization";
import { PatientProcedureEventItemsPanel } from "./PatientProcedureEventItemsPanel";
import type { ProcedureEventViewer } from "../../../models/patients/procedures/PatientProcedureEventItemModels";
import { usePatientProcedureEventDetail } from "../../../hooks/patients/procedures/usePatientProcedureEventDetail";

interface Props {
  patientId: string;
  eventId: string;
  procedureId: string;
  viewer?: ProcedureEventViewer;
  onDeleted: () => void;
}

export function PatientProcedureEventDetailPanel({
  patientId,
  eventId,
  procedureId,
  viewer,
  onDeleted,
}: Props) {
  const { t, locale } = useLocalizedText();
  const query = usePatientProcedureEventDetail(eventId);
  const capabilitiesQuery = useSeaEventCapabilities(eventId);
  if (query.isLoading) return <p>{t("common.states.loading")}</p>;
  if (query.error || !query.data)
    return <p role="alert">{t("clinic.procedures.eventNotFound")}</p>;

  const event = query.data;
  const canWrite = capabilitiesQuery.data?.canWrite ?? false;
  const details: DetailGridItem[] = [
    {
      label: t("clinic.fields.created"),
      value: formatDateTime(event.createdAt, locale),
    },
    ...(event.committedAt
      ? [
          {
            label: t("clinic.fields.committedAt"),
            value: formatDateTime(event.committedAt, locale),
          },
        ]
      : []),
    ...(event.description
      ? [{ label: t("clinic.fields.description"), value: event.description }]
      : []),
  ];
  return (
    <section
      className="flex min-h-0 flex-1 flex-col gap-6"
      aria-labelledby={`clinical-step-${event.id}`}
    >
      <PatientProcedureEventItemsPanel
        context={{ patientId, procedureId, eventId }}
        canWrite={canWrite}
        viewer={viewer}
      />
    </section>
  );
}

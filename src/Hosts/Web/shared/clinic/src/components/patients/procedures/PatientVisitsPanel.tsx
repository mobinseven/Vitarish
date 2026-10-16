"use client";

import { Button, Chip, Spinner, Tooltip } from "@heroui/react";
import { useEffect } from "react";
import { FaIcon } from "@vitarish/ui";
import { usePatientProcedures } from "../../../hooks/patients/procedures/usePatientProcedures";
import { usePatientProcedureEvents } from "../../../hooks/patients/procedures/usePatientProcedureEvents";
import { patientVisitsProcedureName } from "../../../hooks/patients/procedures/useEnsurePatientVisitsProcedure";
import { formatDate, useLocalizedText } from "../../../localization";
import type { NavigationIntent } from "../../../navigation";
import { clinicOperationButtonClassName } from "../../operations/clinicOperation";

interface PatientVisitsPanelProps {
  patientId: string;
  onVisibilityChange?: (visible: boolean) => void;
  onNewVisit?: () => void;
  onNavigate?: (intent: NavigationIntent) => void;
}

export function PatientVisitsPanel({
  patientId,
  onVisibilityChange,
  onNewVisit,
  onNavigate,
}: PatientVisitsPanelProps) {
  const { t, locale } = useLocalizedText();
  const { data: procedures, isLoading } = usePatientProcedures(patientId);
  const visitsProcedure = procedures?.find(
    (procedure) => procedure.name === patientVisitsProcedureName,
  );
  const eventsQuery = usePatientProcedureEvents(visitsProcedure?.id ?? "");

  useEffect(() => {
    if (eventsQuery.data) {
      onVisibilityChange?.(eventsQuery.data.length > 0);
    }
  }, [eventsQuery.data, onVisibilityChange]);

  if (isLoading) {
    return <Spinner size="sm" aria-label={t("common.states.loading")} />;
  }

  if (!visitsProcedure || (eventsQuery.data && eventsQuery.data.length === 0)) {
    return null;
  }

  return (
    <>
      <div className="flex items-center justify-between gap-3">
        <p className="text-sm text-muted">
          {t("clinic.procedures.visits.description")}
        </p>
        <Button size="sm" variant="tertiary" onPress={onNewVisit}>
          <FaIcon icon="plus" aria-hidden="true" />
          {t("clinic.procedures.visits.new")}
        </Button>
      </div>
      {eventsQuery.data?.length ? (
        <ul className="mt-3 flex flex-col divide-y divide-separator">
          {[...eventsQuery.data].reverse().map((visit) => (
            <li key={visit.id} className="flex items-center justify-between gap-3 py-3">
              <div className="min-w-0">
                <p dir="auto" className="truncate font-medium text-foreground">{visit.title}</p>
                <p className="text-sm text-muted">
                  {formatDate(visit.createdAt, locale, { dateStyle: "medium" })}
                </p>
              </div>
              <div className="flex shrink-0 items-center gap-2">
                <Chip color={visit.isCommitted ? "success" : "default"} size="sm" variant="soft">
                  {t(
                    visit.isCommitted
                      ? "clinic.procedures.events.committedState"
                      : "clinic.procedures.events.pendingState",
                  )}
                </Chip>
                {onNavigate && visitsProcedure ? (
                  <Tooltip>
                    <Button
                      isIconOnly
                      size="sm"
                      variant="tertiary"
                      aria-label={t("clinic.procedures.visits.open")}
                      onPress={() =>
                        onNavigate({
                          kind: "patientProcedureEventDetail",
                          patientId,
                          procedureId: visitsProcedure.id,
                          eventId: visit.id,
                        })
                      }
                    >
                      <FaIcon icon="arrow-up-right-from-square" aria-hidden="true" />
                    </Button>
                    <Tooltip.Content>{t("clinic.procedures.visits.open")}</Tooltip.Content>
                  </Tooltip>
                ) : null}
              </div>
            </li>
          ))}
        </ul>
      ) : null}
    </>
  );
}

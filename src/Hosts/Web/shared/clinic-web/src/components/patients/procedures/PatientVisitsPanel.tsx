"use client";

import { Button, Spinner } from "@heroui/react";
import { useEffect } from "react";
import { FaIcon } from "@vitarish/web-ui";
import { usePatientProcedures } from "../../../hooks/patients/procedures/usePatientProcedures";
import { usePatientProcedureEvents } from "../../../hooks/patients/procedures/usePatientProcedureEvents";
import { patientVisitsProcedureName } from "../../../hooks/patients/procedures/useEnsurePatientVisitsProcedure";
import { useLocalizedText } from "../../../localization";

interface PatientVisitsPanelProps {
  patientId: string;
  onVisibilityChange?: (visible: boolean) => void;
  onNewVisit?: () => void;
}

export function PatientVisitsPanel({
  patientId,
  onVisibilityChange,
  onNewVisit,
}: PatientVisitsPanelProps) {
  const { t } = useLocalizedText();
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
        <Button size="sm" variant="primary" onPress={onNewVisit}>
          <FaIcon icon="plus" aria-hidden="true" />
          {t("clinic.procedures.visits.new")}
        </Button>
      </div>
    </>
  );
}

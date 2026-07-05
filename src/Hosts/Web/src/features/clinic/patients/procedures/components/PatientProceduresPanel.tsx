"use client";

import { ListView, Widget } from "@heroui-pro/react";
import { ClinicEmptyState, ClinicErrorState, ClinicLoadingState } from "@/features/clinic/components/ClinicStateSurface";
import { usePatientProcedures } from "../hooks/useProcedures";
import { useLocalizedText } from "@/localization";

interface PatientProceduresPanelProps {
  patientId: string;
}

export function PatientProceduresPanel({ patientId }: PatientProceduresPanelProps) {
  const { t } = useLocalizedText();
  const { data: procedures, isLoading, error } = usePatientProcedures(patientId);

  return (
    <Widget>
      <Widget.Header>
        <Widget.Title>{t("clinic.procedures.title")}</Widget.Title>
      </Widget.Header>
      <Widget.Content>
        {isLoading && (
          <ClinicLoadingState label={t("common.states.loading")} />
        )}

        {error && (
          <ClinicErrorState title={t("clinic.procedures.failedToLoad")} />
        )}

        {!isLoading && !error && procedures && (
          <ListView
            aria-label={t("clinic.procedures.title")}
            renderEmptyState={() => <ClinicEmptyState title={t("clinic.procedures.comingSoon")} />}
            variant="secondary"
          >
            {procedures.map((procedure) => (
              <ListView.Item key={procedure.id} id={procedure.id} textValue={procedure.name}>
                <ListView.ItemContent>
                  <div className="flex flex-col gap-1">
                    <ListView.Title>{procedure.name}</ListView.Title>
                    {procedure.description && (
                      <ListView.Description>{procedure.description}</ListView.Description>
                    )}
                  </div>
                </ListView.ItemContent>
                {procedure.stats && (
                  <ListView.ItemAction>
                    <span className="text-xs text-muted">
                      {t("clinic.patient.overview.events")}: {Number(procedure.stats.events) || 0}
                    </span>
                  </ListView.ItemAction>
                )}
              </ListView.Item>
            ))}
          </ListView>
        )}
      </Widget.Content>
    </Widget>
  );
}

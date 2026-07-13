"use client";

import { useCallback, useState } from "react";
import { Button, Chip } from "@heroui/react";
import { ListView } from "@heroui-pro/react";
import { EmptyStateSurface as ClinicEmptyState, ErrorStateSurface as ClinicErrorState, LoadingStateSurface as ClinicLoadingState } from "@vitarish/web-ui";
import { usePatientProcedures } from "../../../hooks/patients/procedures/usePatientProcedures";
import { usePatientProcedureTriage } from "../../../hooks/patients/procedures/usePatientProcedureTriage";
import { PatientProcedureForm } from "./PatientProcedureForm";
import { useLocalizedText, formatDateTime } from "../../../localization/index";

interface PatientProceduresPanelProps {
  patientId: string;
  onNavigate?: (intent: import("../../../navigation").ClinicNavigationIntent) => void;
}

export function PatientProceduresPanel({ patientId, onNavigate }: PatientProceduresPanelProps) {
  const { t, locale } = useLocalizedText();
  const { data: procedures, isLoading, error } = usePatientProcedures(patientId);
  const { data: triage } = usePatientProcedureTriage(patientId);
  const [showCreateForm, setShowCreateForm] = useState(false);

  const handleSelect = useCallback(
    (procedureId: string) => onNavigate?.({ kind: "patientProcedureDetail", patientId: patientId, procedureId: procedureId }),
    [patientId, onNavigate]
  );

  return (
    <section className="rounded-medium border border-divider p-4">
      <header className="mb-3">
        <h2>{t("clinic.procedures.title")}</h2>
      </header>
      <div>
        {isLoading && <ClinicLoadingState label={t("common.states.loading")} />}

        {error && <ClinicErrorState title={t("clinic.procedures.failedToLoad")} />}

        {!isLoading && !error && triage && triage.totalProcedures > 0 && (
          <div className="flex flex-wrap gap-2 mb-3">
            {triage.incompleteProcedures > 0 && (
              <Chip size="sm" color="warning" variant="soft">
                {triage.incompleteProcedures} {t("clinic.procedures.triage.incomplete")}
              </Chip>
            )}
            {triage.newEvidence > 0 && (
              <Chip size="sm" color="accent" variant="soft">
                {triage.newEvidence} {t("clinic.procedures.triage.newEvidence")}
              </Chip>
            )}
          </div>
        )}

        {!isLoading && !error && (
          <div className="flex justify-end mb-3">
            <Button size="sm" variant="primary" onPress={() => setShowCreateForm(true)}>
              {t("clinic.procedures.new")}
            </Button>
          </div>
        )}

        {!isLoading && !error && procedures && (
          <ListView
            aria-label={t("clinic.procedures.title")}
            onAction={(key) => handleSelect(String(key))}
            renderEmptyState={() => <ClinicEmptyState title={t("clinic.procedures.empty")} />}
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
                <ListView.ItemAction>
                  <div className="flex items-center gap-2">
                    {procedure.eventCount > 0 && (
                      <span className="text-xs text-muted">
                        {t("clinic.procedures.events.progress", { committed: procedure.committedEventCount, count: procedure.eventCount })}
                      </span>
                    )}
                    {procedure.newItems > 0 && (
                      <Chip size="sm" color="accent" variant="soft">
                        {procedure.newItems}
                      </Chip>
                    )}
                    {procedure.lastActivity && (
                      <span className="text-xs text-muted">
                        {formatDateTime(procedure.lastActivity, locale)}
                      </span>
                    )}
                  </div>
                </ListView.ItemAction>
              </ListView.Item>
            ))}
          </ListView>
        )}

        {showCreateForm && (
          <PatientProcedureForm
            patientId={patientId}
            onClose={() => setShowCreateForm(false)}
          />
        )}
      </div>
    </section>
  );
}

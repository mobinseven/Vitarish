"use client";

import { useState } from "react";
import { Alert, Button, Card, Chip, ListBox, Spinner } from "@heroui/react";
import { usePatientProcedures } from "../../../hooks/patients/procedures/usePatientProcedures";
import { usePatientProcedureTriage } from "../../../hooks/patients/procedures/usePatientProcedureTriage";
import { PatientProcedureForm } from "./PatientProcedureForm";
import { useLocalizedText, formatDateTime } from "../../../localization/index";

interface PatientProceduresPanelProps {
  patientId: string;
  onNavigate?: (
    intent: import("../../../navigation").NavigationIntent,
  ) => void;
}

export function PatientProceduresPanel({
  patientId,
  onNavigate,
}: PatientProceduresPanelProps) {
  const { t, locale } = useLocalizedText();
  const {
    data: procedures,
    isLoading,
    error,
  } = usePatientProcedures(patientId);
  const { data: triage } = usePatientProcedureTriage(patientId);
  const [showCreateForm, setShowCreateForm] = useState(false);

  return (
    <Card variant="secondary">
      <Card.Header className="flex gap-3">
        {!isLoading && !error && (
          <Button
            size="sm"
            variant="primary"
            onPress={() => setShowCreateForm(true)}
          >
            {t("clinic.procedures.new")}
          </Button>
        )}
      </Card.Header>
      <Card.Content className="flex flex-col gap-3">
        {isLoading ? (
          <Spinner
            aria-label={t("common.states.loading")}
            className="self-center"
          />
        ) : error ? (
          <Alert status="danger">
            <Alert.Indicator />
            <Alert.Content>
              <Alert.Title>{t("clinic.procedures.failedToLoad")}</Alert.Title>
            </Alert.Content>
          </Alert>
        ) : (
          <>
            {triage && triage.totalProcedures > 0 && (
              <div className="flex flex-wrap gap-2">
                {triage.incompleteProcedures > 0 && (
                  <Chip size="sm" color="warning" variant="soft">
                    {triage.incompleteProcedures}{" "}
                    {t("clinic.procedures.triage.incomplete")}
                  </Chip>
                )}
                {triage.newEvidence > 0 && (
                  <Chip size="sm" color="accent" variant="soft">
                    {triage.newEvidence}{" "}
                    {t("clinic.procedures.triage.newEvidence")}
                  </Chip>
                )}
              </div>
            )}

            {procedures?.length ? (
              <ListBox
                aria-label={t("clinic.procedures.title")}
                onAction={(key) =>
                  onNavigate?.({
                    kind: "patientProcedureDetail",
                    patientId,
                    procedureId: String(key),
                  })
                }
              >
                {procedures.map((procedure) => (
                  <ListBox.Item
                    key={procedure.id}
                    id={procedure.id}
                    textValue={procedure.name}
                  >
                    <div className="flex min-w-0 flex-1 items-center justify-between gap-3">
                      <div className="min-w-0">
                        <p className="font-medium">{procedure.name}</p>
                        {procedure.description && (
                          <p className="truncate text-sm text-muted">
                            {procedure.description}
                          </p>
                        )}
                      </div>
                      <div className="flex shrink-0 items-center gap-2">
                        {procedure.eventCount > 0 && (
                          <span className="text-xs text-muted">
                            {t("clinic.procedures.events.progress", {
                              committed: procedure.committedEventCount,
                              count: procedure.eventCount,
                            })}
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
                    </div>
                  </ListBox.Item>
                ))}
              </ListBox>
            ) : (
              <Card.Description className="py-6 text-center">
                {t("clinic.procedures.empty")}
              </Card.Description>
            )}
          </>
        )}

        {showCreateForm && (
          <PatientProcedureForm
            patientId={patientId}
            onClose={() => setShowCreateForm(false)}
          />
        )}
      </Card.Content>
    </Card>
  );
}

"use client";

import { Card, Spinner } from "@heroui/react";
import { usePatientProcedures } from "../hooks/useProcedures";
import { useLocalizedText } from "@/localization";

interface PatientProceduresPanelProps {
  patientId: string;
}

export function PatientProceduresPanel({ patientId }: PatientProceduresPanelProps) {
  const { t } = useLocalizedText();
  const { data: procedures, isLoading, error } = usePatientProcedures(patientId);

  return (
    <Card>
      <Card.Header>
        <h2 className="text-lg font-semibold">{t("clinic.procedures.title")}</h2>
      </Card.Header>
      <Card.Content>
        {isLoading && (
          <div className="flex items-center justify-center py-4">
            <Spinner size="sm" />
          </div>
        )}

        {error && (
          <p className="text-sm text-danger">{t("clinic.procedures.failedToLoad")}</p>
        )}

        {!isLoading && !error && procedures && (
          <div className="flex flex-col gap-1">
            {procedures.length === 0 && (
              <p className="py-4 text-center text-sm text-muted">
                {t("clinic.procedures.comingSoon")}
              </p>
            )}
            {procedures.map((p) => (
              <div
                key={p.id}
                className="flex items-center justify-between rounded-lg px-3 py-2 transition-colors hover:bg-surface-secondary"
              >
                <div className="flex flex-col gap-1">
                  <span className="text-sm font-medium">{p.name}</span>
                  {p.description && (
                    <span className="text-xs text-muted">{p.description}</span>
                  )}
                </div>
                {p.stats && (
                  <span className="text-xs text-muted">
                    {t("clinic.patient.overview.events")}: {Number(p.stats.events) || 0}
                  </span>
                )}
              </div>
            ))}
          </div>
        )}
      </Card.Content>
    </Card>
  );
}

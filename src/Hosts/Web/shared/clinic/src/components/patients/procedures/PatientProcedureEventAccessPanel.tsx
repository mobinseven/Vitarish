"use client";

import { Surface } from "@heroui/react";
import { EmptyStateSurface as ClinicEmptyState, HeaderIcon } from "@vitarish/ui";
import { usePatientProcedureEventAccess } from "../../../hooks/patients/procedures/usePatientProcedureEventAccess";
import { useLocalizedText } from "../../../localization/index";

interface PatientProcedureEventAccessPanelProps {
  eventId: string;
}

export function PatientProcedureEventAccessPanel({
  eventId,
}: PatientProcedureEventAccessPanelProps) {
  const { t } = useLocalizedText();
  const { data: access, isLoading } = usePatientProcedureEventAccess(eventId);

  if (isLoading) return null;

  return (
    <Surface className="flex flex-col gap-6 rounded-3xl p-6">
      <div className="flex flex-col items-start gap-1">
        <div className="flex items-center gap-3">
          <HeaderIcon context="visit" icon="key" />
          <div className="min-w-0">
            <h2 className="text-lg font-semibold text-foreground">{t("clinic.procedures.access.title")}</h2>
          </div>
        </div>
      </div>
      <div>
        {!access ||
        (access.readRoles.length === 0 && access.writeRoles.length === 0) ? (
          <ClinicEmptyState title={t("common.states.empty")} />
        ) : (
          <div className="flex flex-col gap-3">
            {access.readRoles.length > 0 && (
              <div>
                <p className="text-sm font-medium mb-1">
                  {t("clinic.procedures.access.read")}
                </p>
                <div className="flex flex-wrap gap-1">
                  {access.readRoles.map((role) => (
                    <span
                      key={role}
                      className="text-xs bg-default-100 px-2 py-1 rounded"
                    >
                      {role}
                    </span>
                  ))}
                </div>
              </div>
            )}
            {access.writeRoles.length > 0 && (
              <div>
                <p className="text-sm font-medium mb-1">
                  {t("clinic.procedures.access.write")}
                </p>
                <div className="flex flex-wrap gap-1">
                  {access.writeRoles.map((role) => (
                    <span
                      key={role}
                      className="text-xs bg-default-100 px-2 py-1 rounded"
                    >
                      {role}
                    </span>
                  ))}
                </div>
              </div>
            )}
          </div>
        )}
      </div>
    </Surface>
  );
}

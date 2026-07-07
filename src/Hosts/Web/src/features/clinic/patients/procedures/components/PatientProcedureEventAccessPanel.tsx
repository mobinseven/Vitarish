"use client";

import { Widget } from "@heroui-pro/react";
import { ClinicEmptyState } from "@/features/clinic/components/ClinicStateSurface";
import { usePatientProcedureEventAccess } from "../hooks/usePatientProcedureEventAccess";
import { useLocalizedText } from "@/localization";

interface PatientProcedureEventAccessPanelProps {
  eventId: string;
}

export function PatientProcedureEventAccessPanel({ eventId }: PatientProcedureEventAccessPanelProps) {
  const { t } = useLocalizedText();
  const { data: access, isLoading } = usePatientProcedureEventAccess(eventId);

  if (isLoading) return null;

  return (
    <Widget>
      <Widget.Header>
        <Widget.Title>{t("clinic.procedures.access.title")}</Widget.Title>
      </Widget.Header>
      <Widget.Content>
        {!access || (access.readRoles.length === 0 && access.writeRoles.length === 0) ? (
          <ClinicEmptyState title={t("clinic.procedures.events.empty")} />
        ) : (
          <div className="flex flex-col gap-3">
            {access.readRoles.length > 0 && (
              <div>
                <p className="text-sm font-medium mb-1">{t("clinic.procedures.access.read")}</p>
                <div className="flex flex-wrap gap-1">
                  {access.readRoles.map((role) => (
                    <span key={role} className="text-xs bg-default-100 px-2 py-1 rounded">
                      {role}
                    </span>
                  ))}
                </div>
              </div>
            )}
            {access.writeRoles.length > 0 && (
              <div>
                <p className="text-sm font-medium mb-1">{t("clinic.procedures.access.write")}</p>
                <div className="flex flex-wrap gap-1">
                  {access.writeRoles.map((role) => (
                    <span key={role} className="text-xs bg-default-100 px-2 py-1 rounded">
                      {role}
                    </span>
                  ))}
                </div>
              </div>
            )}
          </div>
        )}
      </Widget.Content>
    </Widget>
  );
}

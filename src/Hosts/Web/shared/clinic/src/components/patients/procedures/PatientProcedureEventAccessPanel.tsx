"use client";

import { Alert, Chip, Surface } from "@heroui/react";
import { EmptyStateSurface as ClinicEmptyState, HeaderIcon } from "@vitarish/ui";
import { usePatientProcedureEventAccess } from "../../../hooks/patients/procedures/usePatientProcedureEventAccess";
import { useLocalizedText } from "../../../localization/index";

interface PatientProcedureEventAccessPanelProps {
  eventId: string;
}

/** The roles the server grants read and write access to a procedure step. */
export function PatientProcedureEventAccessPanel({
  eventId,
}: PatientProcedureEventAccessPanelProps) {
  const { t } = useLocalizedText();
  const { data: access, error, isLoading } = usePatientProcedureEventAccess(eventId);

  if (isLoading) return null;

  const groups = access
    ? [
        { key: "read", label: t("clinic.procedures.access.read"), roles: access.readRoles },
        { key: "write", label: t("clinic.procedures.access.write"), roles: access.writeRoles },
      ].filter((group) => group.roles.length > 0)
    : [];

  return (
    <Surface className="flex flex-col gap-6 rounded-3xl p-6">
      <div className="flex items-center gap-3">
        <HeaderIcon icon="key" />
        <h2 className="text-lg font-semibold text-foreground">{t("clinic.procedures.access.title")}</h2>
      </div>
      {error ? (
        <Alert status="danger">
          <Alert.Indicator />
          <Alert.Content>
            <Alert.Title>{t("clinic.procedures.access.loadFailed")}</Alert.Title>
          </Alert.Content>
        </Alert>
      ) : groups.length === 0 ? (
        <ClinicEmptyState title={t("common.states.empty")} />
      ) : (
        <div className="flex flex-col gap-4">
          {groups.map((group) => (
            <div key={group.key} className="flex flex-col gap-2">
              <p className="text-sm font-medium text-foreground">{group.label}</p>
              <div className="flex flex-wrap gap-2">
                {group.roles.map((role) => (
                  <Chip key={role} size="sm" variant="soft">
                    {role}
                  </Chip>
                ))}
              </div>
            </div>
          ))}
        </div>
      )}
    </Surface>
  );
}

"use client";

import { Avatar, Card } from "@heroui/react";
import { FaIcon, RevealHeader } from "@vitarish/web-ui";
import type { PatientViewDto } from "../../../models/patients/patientSearchModels";
import type { PatientProcedureDetail } from "../../../models/patients/procedures/PatientProcedureModels";
import { useLocalizedText } from "../../../localization";

interface PatientProcedureHeaderProps {
  patient: PatientViewDto;
  procedure: PatientProcedureDetail;
}

function getDisplayName(patient: PatientViewDto, fallback: string): string {
  const name = [patient.firstName, patient.lastName].filter(Boolean).join(" ").trim();
  return name || patient.name?.trim() || fallback;
}

export function PatientProcedureHeader({ patient, procedure }: PatientProcedureHeaderProps) {
  const { t } = useLocalizedText();
  const displayName = getDisplayName(patient, t("clinic.patients.unnamedPatient"));

  return (
    <Card variant="transparent">
      <Card.Header className="flex flex-col items-center gap-4 px-4 py-5 text-center sm:flex-row sm:text-left">
        <Avatar color="accent" size="lg" variant="soft" role="img" aria-label={displayName}>
          <Avatar.Fallback className="text-lg font-semibold">
            <FaIcon icon="user" />
          </Avatar.Fallback>
        </Avatar>
        <div className="min-w-0 flex-1">
          <div className="flex flex-wrap items-center justify-center gap-2 sm:justify-start">
            <h1 className="text-2xl font-semibold text-foreground sm:text-3xl">{displayName}</h1>
          </div>
          <div className="mt-1 flex flex-wrap justify-center gap-x-3 gap-y-1 text-sm text-muted sm:justify-start">
            <span>{procedure.name}</span>
          </div>
        </div>
      </Card.Header>
      <RevealHeader
        compactContent={
          <div className="flex min-h-14 items-center gap-3 border-b border-border bg-background/95 px-4 shadow-sm backdrop-blur">
            <Avatar color="accent" size="sm" variant="soft" role="img" aria-label={displayName}>
              <Avatar.Fallback>
                <FaIcon icon="user" />
              </Avatar.Fallback>
            </Avatar>
            <div className="min-w-0">
              <p className="truncate font-semibold text-foreground">{displayName}</p>
              <p className="truncate text-sm text-muted">{procedure.name}</p>
            </div>
          </div>
        }
      />
      <RevealHeader.Boundary />
    </Card>
  );
}

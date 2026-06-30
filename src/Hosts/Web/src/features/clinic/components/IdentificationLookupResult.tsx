"use client";

import { Card, Button } from "@heroui/react";
import type { PatientViewDto } from "../types";

interface IdentificationLookupResultProps {
  status: "found" | "not-found" | "error";
  patient: PatientViewDto | null;
  error: string | null;
  onSelect: (patientId: string) => void;
  onRetry: () => void;
}

export function IdentificationLookupResult({
  status,
  patient,
  error,
  onSelect,
  onRetry,
}: IdentificationLookupResultProps) {
  if (status === "found" && patient) {
    return (
      <Card className="glass-card p-4">
        <div className="flex items-center justify-between">
          <div>
            <p className="font-medium">
              {patient.firstName} {patient.lastName}
            </p>
            <p className="text-sm text-default-500">
              {patient.identificationCode && `ID: ${patient.identificationCode}`}
              {patient.phone && ` | ${patient.phone}`}
            </p>
          </div>
          <Button variant="primary" onPress={() => patient.id && onSelect(patient.id)}>
            Select
          </Button>
        </div>
      </Card>
    );
  }

  if (status === "not-found") {
    return (
      <Card className="glass-card p-4">
        <div className="flex items-center justify-between">
          <p className="text-default-500">Patient not found</p>
          <Button variant="ghost" onPress={onRetry}>
            Retry
          </Button>
        </div>
      </Card>
    );
  }

  if (status === "error") {
    return (
      <Card className="glass-card p-4">
        <div className="flex items-center justify-between">
          <p className="text-danger">{error ?? "Lookup failed"}</p>
          <Button variant="ghost" onPress={onRetry}>
            Retry
          </Button>
        </div>
      </Card>
    );
  }

  return null;
}

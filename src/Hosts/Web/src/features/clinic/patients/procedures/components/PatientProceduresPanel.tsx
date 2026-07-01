"use client";

import { Card } from "@heroui/react";

interface PatientProceduresPanelProps {
  patientId: string;
}

export function PatientProceduresPanel({ patientId: _patientId }: PatientProceduresPanelProps) {
  return (
    <Card className="glass-card">
      <Card.Header>
        <h2 className="text-lg font-semibold">Procedures</h2>
      </Card.Header>
      <Card.Content>
        <p className="text-sm text-muted">
          Procedures management for this patient will be available here soon.
        </p>
      </Card.Content>
    </Card>
  );
}

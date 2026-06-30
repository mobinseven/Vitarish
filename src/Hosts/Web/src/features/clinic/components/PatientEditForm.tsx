"use client";

import { Card, Button } from "@heroui/react";

interface PatientEditFormProps {
  patientId: string;
  onCancel: () => void;
}

export function PatientEditForm({ onCancel }: PatientEditFormProps) {
  return (
    <Card className="glass-card">
      <Card.Header>
        <h2 className="text-lg font-semibold">Edit Patient</h2>
      </Card.Header>
      <Card.Content className="flex flex-col gap-4">
        <p className="text-default-500">
          Patient editing is not yet available. This feature requires a backend
          endpoint that has not been implemented yet.
        </p>
        <div className="flex justify-end gap-2">
          <Button variant="ghost" onPress={onCancel}>
            Cancel
          </Button>
        </div>
      </Card.Content>
    </Card>
  );
}

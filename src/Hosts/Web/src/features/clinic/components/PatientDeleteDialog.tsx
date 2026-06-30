"use client";

import { Card, Button } from "@heroui/react";

interface PatientDeleteDialogProps {
  isOpen: boolean;
  onClose: () => void;
  patientId: string;
}

export function PatientDeleteDialog({
  isOpen,
  onClose,
}: PatientDeleteDialogProps) {
  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <Card className="glass-card w-full max-w-md p-6">
        <Card.Header>
          <h2 className="text-lg font-semibold">Delete Patient</h2>
        </Card.Header>
        <Card.Content className="flex flex-col gap-4">
          <p className="text-default-500">
            Patient deletion is not yet available. This feature requires a
            backend endpoint that has not been implemented yet.
          </p>
          <div className="flex justify-end gap-2">
            <Button variant="ghost" onPress={onClose}>
              Close
            </Button>
          </div>
        </Card.Content>
      </Card>
    </div>
  );
}

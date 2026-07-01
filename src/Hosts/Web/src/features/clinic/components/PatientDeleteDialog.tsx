"use client";

import { Modal, Button } from "@heroui/react";

interface PatientDeleteDialogProps {
  isOpen: boolean;
  onClose: () => void;
  patientId: string;
}

export function PatientDeleteDialog({
  isOpen,
  onClose,
}: PatientDeleteDialogProps) {
  return (
    <Modal isOpen={isOpen} onOpenChange={onClose}>
      <Modal.Content>
        <Modal.Header>
          <h2 className="text-lg font-semibold">Delete Patient</h2>
        </Modal.Header>
        <Modal.Body>
          <p className="text-sm text-muted">
            Patient deletion is currently unavailable. Please contact support if you need assistance.
          </p>
        </Modal.Body>
        <Modal.Footer>
          <Button variant="secondary" onPress={onClose}>
            Close
          </Button>
        </Modal.Footer>
      </Modal.Content>
    </Modal>
  );
}

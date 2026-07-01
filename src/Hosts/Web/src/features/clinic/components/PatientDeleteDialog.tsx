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
    <Modal>
      <Modal.Backdrop isOpen={isOpen} onOpenChange={onClose}>
        <Modal.Container>
          <Modal.Dialog className="sm:max-w-md">
            <Modal.CloseTrigger />
            <Modal.Header>
              <Modal.Heading>Delete Patient</Modal.Heading>
            </Modal.Header>
            <Modal.Body>
              <p className="text-sm text-muted">
                Patient deletion is currently unavailable. Please contact support if you need assistance.
              </p>
            </Modal.Body>
            <Modal.Footer>
              <Button slot="close" variant="secondary">
                Close
              </Button>
            </Modal.Footer>
          </Modal.Dialog>
        </Modal.Container>
      </Modal.Backdrop>
    </Modal>
  );
}

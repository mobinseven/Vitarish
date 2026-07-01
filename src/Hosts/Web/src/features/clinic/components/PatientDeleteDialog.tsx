"use client";

import { useMemo } from "react";
import { Modal, Button } from "@heroui/react";
import { useLocalizedText } from "@/localization";

interface PatientDeleteDialogProps {
  isOpen: boolean;
  onClose: () => void;
  patientId: string;
}

export function PatientDeleteDialog({
  isOpen,
  onClose,
}: PatientDeleteDialogProps) {
  const { t } = useLocalizedText();
  const modalState = useMemo(
    () => ({
      isOpen,
      setOpen: (open: boolean) => {
        if (!open) onClose();
      },
      open: () => undefined,
      close: onClose,
      toggle: () => {
        if (isOpen) onClose();
      },
    }),
    [isOpen, onClose]
  );

  if (!isOpen) {
    return null;
  }

  return (
    <Modal state={modalState}>
      <Modal.Backdrop isDismissable>
        <Modal.Container placement="center" size="md">
          <Modal.Dialog>
            <Modal.Header>
              <Modal.Heading className="text-lg font-semibold">
                {t("clinic.patient.delete.title")}
              </Modal.Heading>
            </Modal.Header>
            <Modal.Body>
              <p className="text-sm text-muted">
                {t("clinic.patient.delete.unavailable")}
              </p>
            </Modal.Body>
            <Modal.Footer>
              <Button variant="secondary" onPress={onClose}>
                {t("common.actions.close")}
              </Button>
            </Modal.Footer>
          </Modal.Dialog>
        </Modal.Container>
      </Modal.Backdrop>
    </Modal>
  );
}

"use client";

import { useMemo } from "react";
import { Button, Modal } from "@heroui/react";
import { useDeletePatientProcedureEvent } from "../hooks/usePatientProcedureEventMutations";
import { useLocalizedText } from "@/localization";

interface PatientProcedureEventDeleteDialogProps {
  eventId: string;
  eventTitle: string;
  onClose: () => void;
  onDeleted: () => void;
}

export function PatientProcedureEventDeleteDialog({
  eventId,
  eventTitle,
  onClose,
  onDeleted,
}: PatientProcedureEventDeleteDialogProps) {
  const { t } = useLocalizedText();
  const deleteMutation = useDeletePatientProcedureEvent();

  const modalState = useMemo(
    () => ({
      isOpen: true,
      setOpen: (open: boolean) => {
        if (!open) onClose();
      },
      open: () => undefined,
      close: onClose,
      toggle: () => onClose(),
    }),
    [onClose]
  );

  const handleConfirm = () => {
    deleteMutation.mutate(eventId, {
      onSuccess: () => onDeleted(),
    });
  };

  return (
    <Modal state={modalState}>
      <Modal.Backdrop isDismissable>
        <Modal.Container placement="center">
          <Modal.Dialog>
            <Modal.Header>
              <Modal.Heading className="text-lg font-semibold">
                {t("clinic.procedures.events.delete.title")}
              </Modal.Heading>
            </Modal.Header>
            <Modal.Body>
              <p>{t("clinic.procedures.events.delete.confirm")}</p>
              <p className="text-sm text-muted">{eventTitle}</p>
            </Modal.Body>
            <Modal.Footer>
              <Button variant="secondary" onPress={onClose} isDisabled={deleteMutation.isPending}>
                {t("common.actions.cancel")}
              </Button>
              <Button variant="danger" onPress={handleConfirm} isDisabled={deleteMutation.isPending}>
                {deleteMutation.isPending ? t("common.states.saving") : t("common.actions.delete")}
              </Button>
            </Modal.Footer>
          </Modal.Dialog>
        </Modal.Container>
      </Modal.Backdrop>
    </Modal>
  );
}

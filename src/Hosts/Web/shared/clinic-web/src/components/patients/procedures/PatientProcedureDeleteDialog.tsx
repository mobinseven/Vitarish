"use client";

import { useMemo } from "react";
import { Button, Modal } from "@heroui/react";
import { useDeletePatientProcedure } from "../../../hooks/patients/procedures/usePatientProcedureMutations";
import { useLocalizedText } from "../../../localization/index";

interface PatientProcedureDeleteDialogProps {
  procedureId: string;
  procedureName: string;
  eventCount: number;
  onClose: () => void;
  onDeleted: () => void;
}

export function PatientProcedureDeleteDialog({
  procedureId,
  procedureName,
  eventCount,
  onClose,
  onDeleted,
}: PatientProcedureDeleteDialogProps) {
  const { t } = useLocalizedText();
  const deleteMutation = useDeletePatientProcedure();

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
    [onClose],
  );

  const handleConfirm = () => {
    deleteMutation.mutate(procedureId, {
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
                {t("clinic.procedures.delete.title")}
              </Modal.Heading>
            </Modal.Header>
            <Modal.Body>
              <p>{t("clinic.procedures.delete.confirm")}</p>
              <p className="text-sm text-muted">
                {t("clinic.procedures.delete.consequences", {
                  name: procedureName,
                  events: String(eventCount),
                })}
              </p>
            </Modal.Body>
            <Modal.Footer>
              <Button
                variant="secondary"
                onPress={onClose}
                isDisabled={deleteMutation.isPending}
              >
                {t("common.actions.cancel")}
              </Button>
              <Button
                variant="danger"
                onPress={handleConfirm}
                isPending={deleteMutation.isPending}
              >
                {t("common.actions.delete")}
              </Button>
            </Modal.Footer>
          </Modal.Dialog>
        </Modal.Container>
      </Modal.Backdrop>
    </Modal>
  );
}

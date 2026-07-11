"use client";

import { useMemo } from "react";
import { Modal, Button } from "@heroui/react";
import { useLocalizedText } from "../../../../localization";
import { useDeleteSeaProcedure } from "../hooks/useDeleteSeaProcedure";
import type { SeaProcedureReadDto } from "../../shared/models/seaStatModels";

interface SeaProcedureDeleteDialogProps {
  isOpen: boolean;
  onClose: () => void;
  procedure: SeaProcedureReadDto;
}

export function SeaProcedureDeleteDialog({ isOpen, onClose, procedure }: SeaProcedureDeleteDialogProps) {
  const { t } = useLocalizedText();
  const deleteMutation = useDeleteSeaProcedure();

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

  if (!isOpen) return null;

  return (
    <Modal state={modalState}>
      <Modal.Backdrop isDismissable>
        <Modal.Container placement="center" size="md">
          <Modal.Dialog>
            <Modal.Header>
              <Modal.Heading className="text-lg font-semibold">
                {t("sea.procedures.deleteProcedure")}
              </Modal.Heading>
            </Modal.Header>
            <Modal.Body>
              <p className="text-sm text-muted">
                {t("sea.procedures.confirmDelete")}
              </p>
              <p className="text-sm font-medium">{procedure.name}</p>
            </Modal.Body>
            <Modal.Footer>
              <Button variant="secondary" onPress={onClose} isDisabled={deleteMutation.isPending}>
                {t("common.actions.cancel")}
              </Button>
              <Button
                variant="danger"
                onPress={() => {
                  deleteMutation.mutate(procedure.id, {
                    onSuccess: onClose,
                  });
                }}
                isDisabled={deleteMutation.isPending}
              >
                {deleteMutation.isPending ? t("common.states.saving") : t("common.actions.delete")}
              </Button>
            </Modal.Footer>
          </Modal.Dialog>
        </Modal.Container>
      </Modal.Backdrop>
    </Modal>
  );
}

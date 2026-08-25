"use client";

import { useMemo } from "react";
import { Modal, Button } from "@heroui/react";
import { useLocalizedText } from "../../localization";
import { useDeleteSeaProject } from "../../hooks/projects/useDeleteSeaProject";
import type { SeaProjectReadDto } from "../../models/seaStatModels";

interface SeaProjectDeleteDialogProps {
  isOpen: boolean;
  onClose: () => void;
  onDeleted?: () => void;
  project: SeaProjectReadDto;
}

export function SeaProjectDeleteDialog({
  isOpen,
  onClose,
  onDeleted,
  project,
}: SeaProjectDeleteDialogProps) {
  const { t } = useLocalizedText();
  const deleteMutation = useDeleteSeaProject();

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
    [isOpen, onClose],
  );

  if (!isOpen) return null;

  return (
    <Modal state={modalState}>
      <Modal.Backdrop isDismissable>
        <Modal.Container placement="center" size="md">
          <Modal.Dialog>
            <Modal.Header>
              <Modal.Heading className="text-lg font-semibold">
                {t("sea.projects.deleteProject")}
              </Modal.Heading>
            </Modal.Header>
            <Modal.Body>
              <p className="text-sm text-muted">
                {t("sea.projects.confirmDelete")}
              </p>
              <p className="text-sm font-medium">{project.name}</p>
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
                onPress={() => {
                  deleteMutation.mutate(project.id, {
                    onSuccess: () => {
                      onClose();
                      onDeleted?.();
                    },
                  });
                }}
                isDisabled={deleteMutation.isPending}
              >
                {deleteMutation.isPending
                  ? t("common.states.saving")
                  : t("common.actions.delete")}
              </Button>
            </Modal.Footer>
          </Modal.Dialog>
        </Modal.Container>
      </Modal.Backdrop>
    </Modal>
  );
}

"use client";
import { useMemo } from "react";
import { Button, Modal } from "@heroui/react";
import { useLocalizedText } from "../../localization/index";

export interface ServiceTypeDeleteDialogProps {
  isOpen: boolean;
  isPending: boolean;
  onCancel: () => void;
  onConfirm: () => void;
}
export function ServiceTypeDeleteDialog({
  isOpen,
  isPending,
  onCancel,
  onConfirm,
}: ServiceTypeDeleteDialogProps) {
  const { t } = useLocalizedText();
  const state = useMemo(
    () => ({
      isOpen,
      setOpen: (open: boolean) => {
        if (!open && !isPending) onCancel();
      },
      open: () => undefined,
      close: onCancel,
      toggle: () => {
        if (isOpen && !isPending) onCancel();
      },
    }),
    [isOpen, isPending, onCancel],
  );
  if (!isOpen) return null;
  return (
    <Modal state={state}>
      <Modal.Backdrop isDismissable={!isPending}>
        <Modal.Container placement="center">
          <Modal.Dialog>
            <Modal.Header>
              <Modal.Heading>
                {t("clinic.serviceTypes.deleteTitle")}
              </Modal.Heading>
            </Modal.Header>
            <Modal.Body>
              <p>{t("clinic.serviceTypes.confirmDelete")}</p>
            </Modal.Body>
            <Modal.Footer>
              <Button
                variant="secondary"
                onPress={onCancel}
                isDisabled={isPending}
              >
                {t("common.actions.cancel")}
              </Button>
              <Button
                variant="danger"
                onPress={onConfirm}
                isPending={isPending}
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

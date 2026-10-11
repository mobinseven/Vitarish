"use client";
import { useMemo } from "react";
import { Button, Modal } from "@heroui/react";
import { useLocalizedText } from "../../../localization/index";

export interface ReservationDeleteDialogProps {
  isOpen: boolean;
  isPending: boolean;
  onCancel: () => void;
  onConfirm: () => void;
}

export function ReservationDeleteDialog({
  isOpen,
  isPending,
  onCancel,
  onConfirm,
}: ReservationDeleteDialogProps) {
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
                {t("clinic.appointments.reservations.deleteTitle")}
              </Modal.Heading>
            </Modal.Header>
            <Modal.Body>
              <p>{t("clinic.appointments.reservations.confirmDelete")}</p>
            </Modal.Body>
            <Modal.Footer>
              <Button
                variant="tertiary"
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

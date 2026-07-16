"use client";

import { useMemo } from "react";
import { Button, Modal } from "@heroui/react";
import { useLocalizedText } from "../../../localization/index";

interface PatientProcedureEventCommitDialogProps {
  eventTitle: string;
  isCommitted: boolean;
  isPending: boolean;
  onClose: () => void;
  onConfirm: () => void;
}

export function PatientProcedureEventCommitDialog({
  eventTitle,
  isCommitted,
  isPending,
  onClose,
  onConfirm,
}: PatientProcedureEventCommitDialogProps) {
  const { t } = useLocalizedText();
  const modalState = useMemo(
    () => ({
      isOpen: true,
      setOpen: (open: boolean) => {
        if (!open && !isPending) onClose();
      },
      open: () => undefined,
      close: onClose,
      toggle: () => {
        if (!isPending) onClose();
      },
    }),
    [isPending, onClose],
  );

  return (
    <Modal state={modalState}>
      <Modal.Backdrop isDismissable={!isPending}>
        <Modal.Container placement="center">
          <Modal.Dialog>
            <Modal.Header>
              <Modal.Heading>{t(isCommitted
                ? "clinic.procedures.events.commitConfirmation.commitTitle"
                : "clinic.procedures.events.commitConfirmation.reopenTitle")}</Modal.Heading>
            </Modal.Header>
            <Modal.Body>
              <p>{t(isCommitted
                ? "clinic.procedures.events.commitConfirmation.commitMessage"
                : "clinic.procedures.events.commitConfirmation.reopenMessage")}</p>
              <p className="text-sm text-muted">{eventTitle}</p>
            </Modal.Body>
            <Modal.Footer>
              <Button variant="secondary" onPress={onClose} isDisabled={isPending}>
                {t("common.actions.cancel")}
              </Button>
              <Button
                className={isCommitted ? "bg-success text-success-foreground hover:bg-success-hover" : undefined}
                variant={isCommitted ? undefined : "primary"}
                onPress={onConfirm}
                isPending={isPending}
              >
                {t(isCommitted
                  ? "clinic.procedures.events.commitConfirmation.confirmCommit"
                  : "clinic.procedures.events.commitConfirmation.confirmReopen")}
              </Button>
            </Modal.Footer>
          </Modal.Dialog>
        </Modal.Container>
      </Modal.Backdrop>
    </Modal>
  );
}

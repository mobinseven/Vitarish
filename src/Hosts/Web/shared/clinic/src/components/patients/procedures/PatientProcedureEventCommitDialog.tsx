"use client";

import { AlertDialog, Button } from "@heroui/react";
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
  return (
    <AlertDialog
      isOpen
      onOpenChange={(isOpen) => {
        if (!isOpen && !isPending) onClose();
      }}
    >
      <Button className="hidden" aria-hidden />
      <AlertDialog.Backdrop isDismissable={!isPending}>
        <AlertDialog.Container placement="center">
          <AlertDialog.Dialog>
            <AlertDialog.Header>
              <AlertDialog.Icon status={isCommitted ? "success" : "default"} />
              <AlertDialog.Heading>
                {t(
                  isCommitted
                    ? "clinic.procedures.events.commitConfirmation.commitTitle"
                    : "clinic.procedures.events.commitConfirmation.reopenTitle",
                )}
              </AlertDialog.Heading>
            </AlertDialog.Header>
            <AlertDialog.Body>
              <p>
                {t(
                  isCommitted
                    ? "clinic.procedures.events.commitConfirmation.commitMessage"
                    : "clinic.procedures.events.commitConfirmation.reopenMessage",
                )}
              </p>
              <p className="text-sm text-muted">{eventTitle}</p>
            </AlertDialog.Body>
            <AlertDialog.Footer>
              <Button
                variant="secondary"
                onPress={onClose}
                isDisabled={isPending}
              >
                {t("common.actions.cancel")}
              </Button>
              <Button
                className={
                  isCommitted
                    ? "bg-success text-success-foreground hover:bg-success-hover"
                    : undefined
                }
                variant={isCommitted ? undefined : "primary"}
                onPress={onConfirm}
                isPending={isPending}
              >
                {t(
                  isCommitted
                    ? "clinic.procedures.events.commitConfirmation.confirmCommit"
                    : "clinic.procedures.events.commitConfirmation.confirmReopen",
                )}
              </Button>
            </AlertDialog.Footer>
          </AlertDialog.Dialog>
        </AlertDialog.Container>
      </AlertDialog.Backdrop>
    </AlertDialog>
  );
}

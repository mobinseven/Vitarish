"use client";

import { Alert, AlertDialog, Button, Tooltip } from "@heroui/react";
import { FaIcon } from "@vitarish/ui";
import {
  useCommitPatientProcedureEvent,
  useUncommitPatientProcedureEvent,
} from "../../../hooks/patients/procedures/usePatientProcedureEventMutations";
import { useLocalizedText } from "../../../localization/index";

interface PatientProcedureEventCommitDialogProps {
  eventId: string;
  eventTitle: string;
  /** The step's current state: a committed step is reopened, an open one is marked complete. */
  isCommitted: boolean;
  onClose: () => void;
}

/** Confirms marking a procedure step complete or reopening it, and keeps the dialog open on failure. */
export function PatientProcedureEventCommitDialog({
  eventId,
  eventTitle,
  isCommitted,
  onClose,
}: PatientProcedureEventCommitDialogProps) {
  const { t } = useLocalizedText();
  const commit = useCommitPatientProcedureEvent();
  const uncommit = useUncommitPatientProcedureEvent();
  const mutation = isCommitted ? uncommit : commit;
  const isPending = mutation.isPending;
  const confirm = () => mutation.mutate(eventId, { onSuccess: onClose });
  const confirmLabel = t(
    isCommitted
      ? "clinic.procedures.events.commitConfirmation.confirmReopen"
      : "clinic.procedures.events.commitConfirmation.confirmCommit",
  );

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
              <AlertDialog.Icon status={isCommitted ? "default" : "success"} />
              <AlertDialog.Heading>
                {t(
                  isCommitted
                    ? "clinic.procedures.events.commitConfirmation.reopenTitle"
                    : "clinic.procedures.events.commitConfirmation.commitTitle",
                )}
              </AlertDialog.Heading>
            </AlertDialog.Header>
            <AlertDialog.Body>
              <p>
                {t(
                  isCommitted
                    ? "clinic.procedures.events.commitConfirmation.reopenMessage"
                    : "clinic.procedures.events.commitConfirmation.commitMessage",
                )}
              </p>
              <p className="text-sm text-muted">{eventTitle}</p>
              {mutation.isError ? (
                <Alert status="danger">
                  <Alert.Indicator />
                  <Alert.Content>
                    <Alert.Title>
                      {t("clinic.procedures.events.commitConfirmation.failed")}
                    </Alert.Title>
                  </Alert.Content>
                </Alert>
              ) : null}
            </AlertDialog.Body>
            <AlertDialog.Footer>
              <Tooltip>
                <Button
                  variant="tertiary"
                  onPress={onClose}
                  isDisabled={isPending}
                >
                  <FaIcon icon="xmark" aria-hidden="true" />
                  {t("common.actions.cancel")}
                </Button>
                <Tooltip.Content>{t("common.actions.cancel")}</Tooltip.Content>
              </Tooltip>
              <Tooltip>
                <Button
                  className={
                    isCommitted
                      ? undefined
                      : "bg-success text-success-foreground hover:bg-success-hover"
                  }
                  variant={isCommitted ? "primary" : undefined}
                  onPress={confirm}
                  isPending={isPending}
                >
                  <FaIcon icon={isCommitted ? "arrow-rotate-left" : "check"} aria-hidden="true" />
                  {confirmLabel}
                </Button>
                <Tooltip.Content>{confirmLabel}</Tooltip.Content>
              </Tooltip>
            </AlertDialog.Footer>
          </AlertDialog.Dialog>
        </AlertDialog.Container>
      </AlertDialog.Backdrop>
    </AlertDialog>
  );
}

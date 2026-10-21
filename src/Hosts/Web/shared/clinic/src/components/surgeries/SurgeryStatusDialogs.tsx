"use client";

import { useState } from "react";
import { Alert, AlertDialog, Button, Tooltip } from "@heroui/react";
import { FaIcon } from "@vitarish/ui";
import { localizeApiError, useLocalizedText } from "../../localization";
import {
  isSurgeryConflictError,
  isSurgeryForbiddenError,
  useSurgeryMutations,
} from "../../hooks/surgeries/useSurgeryMutations";
import type { Surgery } from "../../models/surgeries/surgeryModels";

/** The status change a surgery detail dialog confirms. */
export type SurgeryStatusAction = "complete" | "cancel";

export interface SurgeryStatusDialogsProps {
  surgery: Surgery;
  /** The dialog to show, or null when none is open. */
  action: SurgeryStatusAction | null;
  onClose: () => void;
  /** Refetches the surgery after a stale conflict. */
  onRefetch: () => void;
}

/**
 * The complete and cancel confirmations for a surgery. Each explains its
 * consequence, blocks while pending, and reports a stale write so the user can
 * review the latest version.
 */
export function SurgeryStatusDialogs({
  surgery,
  action,
  onClose,
  onRefetch,
}: SurgeryStatusDialogsProps) {
  const { t } = useLocalizedText();
  const { complete, cancel } = useSurgeryMutations();
  const [error, setError] = useState<string | null>(null);

  if (!action) return null;

  const isComplete = action === "complete";
  const mutation = isComplete ? complete : cancel;

  const handleClose = () => {
    if (mutation.isPending) return;
    setError(null);
    onClose();
  };

  const handleConfirm = () => {
    setError(null);
    mutation.mutate(surgery, {
      onSuccess: () => {
        onClose();
        onRefetch();
      },
      onError: (err) => {
        if (isSurgeryConflictError(err)) {
          setError(t("clinic.surgeries.detail.dialogs.conflict"));
          onRefetch();
          return;
        }
        setError(
          isSurgeryForbiddenError(err)
            ? t("clinic.surgeries.errors.ownerOnly")
            : localizeApiError(err, t),
        );
      },
    });
  };

  const title = isComplete
    ? t("clinic.surgeries.detail.dialogs.completeTitle")
    : t("clinic.surgeries.detail.dialogs.cancelTitle");
  const message = isComplete
    ? t("clinic.surgeries.detail.dialogs.completeMessage")
    : t("clinic.surgeries.detail.dialogs.cancelMessage");
  const confirmLabel = isComplete
    ? t("clinic.surgeries.detail.dialogs.completeConfirm")
    : t("clinic.surgeries.detail.dialogs.cancelConfirm");

  return (
    <AlertDialog
      isOpen
      onOpenChange={(isOpen) => {
        if (!isOpen) handleClose();
      }}
    >
      <Button className="hidden" aria-hidden />
      <AlertDialog.Backdrop isDismissable={!mutation.isPending}>
        <AlertDialog.Container placement="center">
          <AlertDialog.Dialog>
            <AlertDialog.Header>
              <AlertDialog.Icon status={isComplete ? "success" : "danger"} />
              <AlertDialog.Heading>{title}</AlertDialog.Heading>
            </AlertDialog.Header>
            <AlertDialog.Body>
              <p>{message}</p>
              {error ? (
                <Alert status="danger">
                  <Alert.Indicator />
                  <Alert.Content>
                    <Alert.Title>{error}</Alert.Title>
                  </Alert.Content>
                </Alert>
              ) : null}
            </AlertDialog.Body>
            <AlertDialog.Footer>
              <Tooltip>
                <Button
                  variant="tertiary"
                  isDisabled={mutation.isPending}
                  onPress={handleClose}
                >
                  <FaIcon icon="xmark" aria-hidden="true" />
                  {t("common.actions.cancel")}
                </Button>
                <Tooltip.Content>{t("common.actions.cancel")}</Tooltip.Content>
              </Tooltip>
              <Tooltip>
                <Button
                  variant={isComplete ? "primary" : "danger"}
                  isPending={mutation.isPending}
                  onPress={handleConfirm}
                >
                  <FaIcon
                    icon={isComplete ? "circle-check" : "ban"}
                    aria-hidden="true"
                  />
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

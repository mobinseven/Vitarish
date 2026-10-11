"use client";

import { AlertDialog, Button } from "@heroui/react";
import { useDeletePatientProcedureEvent } from "../../../hooks/patients/procedures/usePatientProcedureEventMutations";
import { useLocalizedText } from "../../../localization/index";

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

  const handleConfirm = () => {
    deleteMutation.mutate(eventId, {
      onSuccess: () => onDeleted(),
    });
  };

  return (
    <AlertDialog
      isOpen
      onOpenChange={(isOpen) => {
        if (!isOpen && !deleteMutation.isPending) onClose();
      }}
    >
      <Button className="hidden" aria-hidden />
      <AlertDialog.Backdrop isDismissable={!deleteMutation.isPending}>
        <AlertDialog.Container placement="center">
          <AlertDialog.Dialog>
            <AlertDialog.Header>
              <AlertDialog.Icon status="danger" />
              <AlertDialog.Heading>
                {t("clinic.procedures.events.delete.title")}
              </AlertDialog.Heading>
            </AlertDialog.Header>
            <AlertDialog.Body>
              <p>{t("clinic.procedures.events.delete.confirm")}</p>
              <p className="text-sm text-muted">{eventTitle}</p>
            </AlertDialog.Body>
            <AlertDialog.Footer>
              <Button
                variant="tertiary"
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
            </AlertDialog.Footer>
          </AlertDialog.Dialog>
        </AlertDialog.Container>
      </AlertDialog.Backdrop>
    </AlertDialog>
  );
}

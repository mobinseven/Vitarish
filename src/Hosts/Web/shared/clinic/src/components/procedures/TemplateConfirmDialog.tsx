"use client";

import { AlertDialog, Button, Tooltip } from "@heroui/react";
import { FaIcon } from "@vitarish/ui";
import { useLocalizedText } from "../../localization";

export function TemplateConfirmDialog({
  title,
  message,
  isPending,
  onClose,
  onConfirm,
}: {
  title: string;
  message: string;
  isPending: boolean;
  onClose: () => void;
  onConfirm: () => void;
}) {
  const { t } = useLocalizedText();
  return (
    <AlertDialog
      isOpen
      onOpenChange={(open) => {
        if (!open && !isPending) onClose();
      }}
    >
      <Button className="hidden" aria-hidden />
      <AlertDialog.Backdrop isDismissable={!isPending}>
        <AlertDialog.Container placement="center">
          <AlertDialog.Dialog>
            <AlertDialog.Header>
              <AlertDialog.Icon status="danger" />
              <AlertDialog.Heading>{title}</AlertDialog.Heading>
            </AlertDialog.Header>
            <AlertDialog.Body>
              <p>{message}</p>
            </AlertDialog.Body>
            <AlertDialog.Footer>
              <Tooltip>
                <Button
                  variant="secondary"
                  isDisabled={isPending}
                  onPress={onClose}
                >
                  <FaIcon icon="xmark" aria-hidden="true" />
                  {t("common.actions.cancel")}
                </Button>
                <Tooltip.Content>{t("common.actions.cancel")}</Tooltip.Content>
              </Tooltip>
              <Tooltip>
                <Button
                  variant="danger"
                  isPending={isPending}
                  onPress={onConfirm}
                >
                  <FaIcon icon="trash" aria-hidden="true" />
                  {t("common.actions.delete")}
                </Button>
                <Tooltip.Content>{t("common.actions.delete")}</Tooltip.Content>
              </Tooltip>
            </AlertDialog.Footer>
          </AlertDialog.Dialog>
        </AlertDialog.Container>
      </AlertDialog.Backdrop>
    </AlertDialog>
  );
}

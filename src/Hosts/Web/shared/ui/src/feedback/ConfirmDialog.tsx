"use client";

import { useMemo, type ReactNode } from "react";
import { Button, Modal, Tooltip } from "@heroui/react";
import { FaIcon } from "../icons/FaIcon";

export interface ConfirmDialogProps {
  isOpen: boolean;
  /** While the confirmed action runs the dialog cannot be dismissed. */
  isPending?: boolean;
  title: string;
  message: ReactNode;
  confirmLabel: string;
  cancelLabel: string;
  /** Font Awesome icon name for the confirm button. */
  confirmIcon: string;
  onCancel: () => void;
  onConfirm: () => void;
}

/**
 * Asks the person to confirm an action that is hard to undo, in the app's own dialog instead of the
 * browser's native one, which cannot be styled, localized with the page, or reliably reached by
 * assistive technology and tests. The confirm button is the danger button; cancel is secondary.
 */
export function ConfirmDialog({
  isOpen,
  isPending = false,
  title,
  message,
  confirmLabel,
  cancelLabel,
  confirmIcon,
  onCancel,
  onConfirm,
}: ConfirmDialogProps) {
  const state = useMemo(
    () => ({
      isOpen,
      setOpen: (open: boolean) => {
        if (!open && !isPending) onCancel();
      },
      open: () => undefined,
      close: () => {
        if (!isPending) onCancel();
      },
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
              <Modal.Heading>{title}</Modal.Heading>
            </Modal.Header>
            <Modal.Body>
              <p>{message}</p>
            </Modal.Body>
            <Modal.Footer>
              <Tooltip>
                <Button variant="secondary" onPress={onCancel} isDisabled={isPending}>
                  <FaIcon icon="xmark" aria-hidden="true" />
                  {cancelLabel}
                </Button>
                <Tooltip.Content>{cancelLabel}</Tooltip.Content>
              </Tooltip>
              <Tooltip>
                <Button variant="danger" onPress={onConfirm} isPending={isPending}>
                  <FaIcon icon={confirmIcon} aria-hidden="true" />
                  {confirmLabel}
                </Button>
                <Tooltip.Content>{confirmLabel}</Tooltip.Content>
              </Tooltip>
            </Modal.Footer>
          </Modal.Dialog>
        </Modal.Container>
      </Modal.Backdrop>
    </Modal>
  );
}

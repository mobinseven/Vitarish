"use client";

import { useMemo, useState } from "react";
import { Button, Input, Label, Modal, TextField } from "@heroui/react";
import { useCreatePatientProcedureEvent, useUpdatePatientProcedureEvent } from "../../../hooks/patients/procedures/usePatientProcedureEventMutations";
import { useLocalizedText } from "../../../localization/index";

interface PatientProcedureEventFormProps {
  procedureId: string;
  projectId: string;
  eventId?: string;
  initialTitle?: string;
  initialDescription?: string;
  onClose: () => void;
}

export function PatientProcedureEventForm({
  procedureId,
  projectId,
  eventId,
  initialTitle = "",
  initialDescription = "",
  onClose,
}: PatientProcedureEventFormProps) {
  const { t } = useLocalizedText();
  const [title, setTitle] = useState(initialTitle);
  const [description, setDescription] = useState(initialDescription);
  const [error, setError] = useState<string | null>(null);

  const createMutation = useCreatePatientProcedureEvent();
  const updateMutation = useUpdatePatientProcedureEvent();

  const isEditing = !!eventId;
  const mutation = isEditing ? updateMutation : createMutation;
  const isPending = mutation.isPending;

  const modalState = useMemo(
    () => ({
      isOpen: true,
      setOpen: (open: boolean) => {
        if (!open) onClose();
      },
      open: () => undefined,
      close: onClose,
      toggle: () => onClose(),
    }),
    [onClose]
  );

  const handleSubmit = () => {
    if (!title.trim()) return;
    setError(null);

    mutation.mutate(
      isEditing
        ? { id: eventId, title: title.trim(), description: description.trim() || undefined }
        : { projectId, procedureId, title: title.trim(), description: description.trim() || undefined },
      {
        onSuccess: () => onClose(),
        onError: () => setError(t("clinic.procedures.form.error")),
      }
    );
  };

  return (
    <Modal state={modalState}>
      <Modal.Backdrop isDismissable>
        <Modal.Container placement="center">
          <Modal.Dialog>
            <Modal.Header>
              <Modal.Heading className="text-lg font-semibold">
                {isEditing ? t("clinic.procedures.form.edit") : t("clinic.procedures.events.addStep")}
              </Modal.Heading>
            </Modal.Header>
            <Modal.Body>
              <TextField isRequired value={title} onChange={setTitle}>
                <Label>{t("clinic.procedures.form.nameLabel")}</Label>
                <Input placeholder={t("clinic.procedures.form.namePlaceholder")} />
              </TextField>
              <TextField value={description} onChange={setDescription}>
                <Label>{t("clinic.procedures.form.descriptionLabel")}</Label>
                <Input placeholder={t("clinic.procedures.form.descriptionPlaceholder")} />
              </TextField>
              {error && <p className="text-sm text-danger">{error}</p>}
            </Modal.Body>
            <Modal.Footer>
              <Button variant="secondary" onPress={onClose} isDisabled={isPending}>
                {t("common.actions.cancel")}
              </Button>
              <Button
                variant="primary"
                onPress={handleSubmit}
                isDisabled={!title.trim()}
                isPending={isPending}
              >
                {isEditing ? t("clinic.procedures.form.edit") : t("clinic.procedures.events.addStep")}
              </Button>
            </Modal.Footer>
          </Modal.Dialog>
        </Modal.Container>
      </Modal.Backdrop>
    </Modal>
  );
}

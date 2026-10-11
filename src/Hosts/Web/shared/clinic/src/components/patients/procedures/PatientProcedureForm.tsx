"use client";

import { useMemo, useState } from "react";
import { Button, Input, Label, Modal, TextField } from "@heroui/react";
import {
  useCreatePatientProcedure,
  useUpdatePatientProcedure,
} from "../../../hooks/patients/procedures/usePatientProcedureMutations";
import { useLocalizedText } from "../../../localization/index";
import { clinicOperationButtonClassName } from "../../operations/clinicOperation";

interface PatientProcedureFormProps {
  patientId: string;
  procedureId?: string;
  initialName?: string;
  initialDescription?: string;
  onClose: () => void;
}

export function PatientProcedureForm({
  patientId,
  procedureId,
  initialName = "",
  initialDescription = "",
  onClose,
}: PatientProcedureFormProps) {
  const { t } = useLocalizedText();
  const [name, setName] = useState(initialName);
  const [description, setDescription] = useState(initialDescription);
  const [error, setError] = useState<string | null>(null);

  const createMutation = useCreatePatientProcedure();
  const updateMutation = useUpdatePatientProcedure();

  const isEditing = !!procedureId;
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
    [onClose],
  );

  const handleSubmit = () => {
    if (!name.trim()) return;
    setError(null);

    mutation.mutate(
      isEditing
        ? {
            id: procedureId,
            name: name.trim(),
            description: description.trim() || undefined,
          }
        : {
            projectId: patientId,
            name: name.trim(),
            description: description.trim() || undefined,
          },
      {
        onSuccess: () => onClose(),
        onError: () => setError(t("clinic.procedures.form.error")),
      },
    );
  };

  return (
    <Modal state={modalState}>
      <Modal.Backdrop isDismissable>
        <Modal.Container placement="center">
          <Modal.Dialog>
            <Modal.Header>
              <Modal.Heading className="text-lg font-semibold">
                {isEditing
                  ? t("clinic.procedures.form.edit")
                  : t("clinic.procedures.form.create")}
              </Modal.Heading>
            </Modal.Header>
            <Modal.Body>
              <TextField isRequired value={name} onChange={setName}>
                <Label>{t("clinic.procedures.form.nameLabel")}</Label>
                <Input
                  placeholder={t("clinic.procedures.form.namePlaceholder")}
                />
              </TextField>
              <TextField value={description} onChange={setDescription}>
                <Label>{t("clinic.procedures.form.descriptionLabel")}</Label>
                <Input
                  placeholder={t(
                    "clinic.procedures.form.descriptionPlaceholder",
                  )}
                />
              </TextField>
              {error && <p className="text-sm text-danger">{error}</p>}
            </Modal.Body>
            <Modal.Footer>
              <Button
                variant="tertiary"
                onPress={onClose}
                isDisabled={isPending}
              >
                {t("common.actions.cancel")}
              </Button>
              <Button
                className={clinicOperationButtonClassName("procedure")}
                variant="primary"
                onPress={handleSubmit}
                isDisabled={!name.trim()}
                isPending={isPending}
              >
                {isEditing
                  ? t("clinic.procedures.form.edit")
                  : t("clinic.procedures.form.create")}
              </Button>
            </Modal.Footer>
          </Modal.Dialog>
        </Modal.Container>
      </Modal.Backdrop>
    </Modal>
  );
}

"use client";

import {
  Button,
  Input,
  Label,
  Modal,
  TextArea,
  TextField,
} from "@heroui/react";
import { useEffect, useState } from "react";
import { FaIcon } from "@vitarish/ui";
import { useCreatePatientProcedureEvent } from "../../../hooks/patients/procedures/usePatientProcedureEventMutations";
import { formatDate, useLocalizedText } from "../../../localization";
import { clinicOperationButtonClassName } from "../../operations/clinicOperation";

interface PatientVisitEventDialogProps {
  patientId: string;
  procedureId: string;
  isOpen: boolean;
  onOpenChange: (open: boolean) => void;
  onCreated: (eventId: string) => void;
}

export function PatientVisitEventDialog({
  patientId,
  procedureId,
  isOpen,
  onOpenChange,
  onCreated,
}: PatientVisitEventDialogProps) {
  const { t, locale } = useLocalizedText();
  const createEvent = useCreatePatientProcedureEvent();
  const [title, setTitle] = useState("");
  const [notes, setNotes] = useState("");

  useEffect(() => {
    if (isOpen) {
      setTitle(formatDate(new Date(), locale, { dateStyle: "long" }));
      setNotes("");
      createEvent.reset();
    }
  }, [isOpen, locale]);

  const submit = () => {
    const normalizedTitle = title.trim();
    if (!normalizedTitle) return;

    createEvent.mutate(
      {
        projectId: patientId,
        procedureId,
        title: normalizedTitle,
        description: notes.trim() || undefined,
      },
      {
        onSuccess: (result) => {
          if (!result.entityId) return;
          onOpenChange(false);
          onCreated(result.entityId);
        },
      },
    );
  };

  return (
    <Modal.Backdrop isOpen={isOpen} onOpenChange={onOpenChange}>
      <Modal.Container placement="center" size="sm">
        <Modal.Dialog>
          <Modal.CloseTrigger />
          <Modal.Header>
            <Modal.Icon className="bg-surface-secondary text-foreground">
              <FaIcon icon="calendar-plus" aria-hidden="true" />
            </Modal.Icon>
            <Modal.Heading>
              {t("clinic.procedures.visits.dialog.title")}
            </Modal.Heading>
            <p className="text-sm leading-5 text-muted">
              {t("clinic.procedures.visits.dialog.description")}
            </p>
          </Modal.Header>
          <Modal.Body className="gap-5">
            <TextField isRequired value={title} onChange={setTitle}>
              <Label>{t("clinic.procedures.visits.dialog.titleLabel")}</Label>
              <Input
                autoFocus
                placeholder={t(
                  "clinic.procedures.visits.dialog.titlePlaceholder",
                )}
              />
            </TextField>
            <TextField value={notes} onChange={setNotes}>
              <Label>{t("clinic.procedures.visits.dialog.notesLabel")}</Label>
              <TextArea
                rows={4}
                placeholder={t(
                  "clinic.procedures.visits.dialog.notesPlaceholder",
                )}
                style={{ resize: "vertical" }}
              />
            </TextField>
            {createEvent.error ? (
              <p className="text-sm text-danger" role="alert">
                {t("clinic.procedures.visits.dialog.createFailed")}
              </p>
            ) : null}
          </Modal.Body>
          <Modal.Footer>
            <Button
              variant="tertiary"
              onPress={() => onOpenChange(false)}
              isDisabled={createEvent.isPending}
            >
              {t("common.actions.cancel")}
            </Button>
            <Button className={clinicOperationButtonClassName("visit")}
              variant="primary"
              onPress={submit}
              isPending={createEvent.isPending}
              isDisabled={!title.trim()}
            >
              {t("clinic.procedures.visits.dialog.create")}
            </Button>
          </Modal.Footer>
        </Modal.Dialog>
      </Modal.Container>
    </Modal.Backdrop>
  );
}

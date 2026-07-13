"use client";

import { useMemo, useState } from "react";
import { Button, Modal } from "@heroui/react";
import { ListView } from "@heroui-pro/react";
import { EmptyStateSurface as ClinicEmptyState, LoadingStateSurface as ClinicLoadingState } from "@vitarish/web-ui";
import { usePatientProcedureTemplates } from "../hooks/usePatientProcedureTemplates";
import { PatientProcedureForm } from "./PatientProcedureForm";
import { useLocalizedText } from "../../../../../localization";

interface PatientProcedureTemplateSelectorProps {
  patientId: string;
  onClose: () => void;
}

export function PatientProcedureTemplateSelector({
  patientId,
  onClose,
}: PatientProcedureTemplateSelectorProps) {
  const { t } = useLocalizedText();
  const { data: templates, isLoading } = usePatientProcedureTemplates();
  const [selectedTemplate, setSelectedTemplate] = useState<{ name: string; description?: string } | null>(null);

  const modalState = useMemo(
    () => ({
      isOpen: !selectedTemplate,
      setOpen: (open: boolean) => {
        if (!open) onClose();
      },
      open: () => undefined,
      close: onClose,
      toggle: () => onClose(),
    }),
    [onClose, selectedTemplate]
  );

  if (selectedTemplate) {
    return (
      <PatientProcedureForm
        patientId={patientId}
        initialName={selectedTemplate.name}
        initialDescription={selectedTemplate.description}
        onClose={onClose}
      />
    );
  }

  return (
    <Modal state={modalState}>
      <Modal.Backdrop isDismissable>
        <Modal.Container placement="center">
          <Modal.Dialog>
            <Modal.Header>
              <Modal.Heading className="text-lg font-semibold">
                {t("clinic.procedures.templates.title")}
              </Modal.Heading>
            </Modal.Header>
            <Modal.Body>
              {isLoading && <ClinicLoadingState label={t("common.states.loading")} />}

              {!isLoading && (!templates || templates.length === 0) && (
                <ClinicEmptyState title={t("clinic.procedures.empty")} />
              )}

              {!isLoading && templates && templates.length > 0 && (
                <ListView
                  aria-label={t("clinic.procedures.templates.title")}
                  onAction={(key) => {
                    const template = templates.find((p) => p.id === String(key));
                    if (template) {
                      setSelectedTemplate({ name: template.name, description: template.description ?? undefined });
                    }
                  }}
                  variant="secondary"
                >
                  {templates.map((template) => (
                    <ListView.Item key={template.id} id={template.id} textValue={template.name}>
                      <ListView.ItemContent>
                        <div className="flex flex-col gap-1">
                          <ListView.Title>{template.name}</ListView.Title>
                          {template.description && (
                            <ListView.Description>{template.description}</ListView.Description>
                          )}
                        </div>
                      </ListView.ItemContent>
                    </ListView.Item>
                  ))}
                </ListView>
              )}
            </Modal.Body>
            <Modal.Footer>
              <Button variant="secondary" onPress={onClose}>
                {t("common.actions.cancel")}
              </Button>
            </Modal.Footer>
          </Modal.Dialog>
        </Modal.Container>
      </Modal.Backdrop>
    </Modal>
  );
}

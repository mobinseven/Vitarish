"use client";

import { useMemo, useState } from "react";
import {
  Alert,
  Button,
  Chip,
  Disclosure,
  Input,
  Label,
  Modal,
  TextField,
  Tooltip,
} from "@heroui/react";
import { FaIcon } from "@vitarish/web-ui";
import {
  EmptyStateSurface as ClinicEmptyState,
  LoadingStateSurface as ClinicLoadingState,
} from "@vitarish/web-ui";
import { usePatientProcedureTemplates } from "../../../hooks/patients/procedures/usePatientProcedureTemplates";
import { useCreatePatientProcedure } from "../../../hooks/patients/procedures/usePatientProcedureMutations";
import { useLocalizedText } from "../../../localization/index";
import type { SeaProcedureReadDto } from "@vitarish/sea-web";

interface PatientProcedureTemplateSelectorProps {
  patientId: string;
  onClose: () => void;
}

export function PatientProcedureTemplateSelector({
  patientId,
  onClose,
}: PatientProcedureTemplateSelectorProps) {
  const { t } = useLocalizedText();
  const { data: groups, isLoading, error } = usePatientProcedureTemplates();
  const createMutation = useCreatePatientProcedure();
  const [selectedProcedure, setSelectedProcedure] = useState<SeaProcedureReadDto | null>(null);
  const [customName, setCustomName] = useState("");
  const [expandedProjectId, setExpandedProjectId] = useState<string | null>(null);
  const [creationError, setCreationError] = useState(false);

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

  const handleSelect = (procedure: SeaProcedureReadDto) => {
    setSelectedProcedure(procedure);
    setCustomName(procedure.name);
  };

  const handleCreate = () => {
    if (!selectedProcedure) return;
    createMutation.mutate(
      {
        projectId: patientId,
        name: customName.trim() || selectedProcedure.name,
        description: selectedProcedure.description ?? undefined,
        templateProcedureId: selectedProcedure.id,
      },
      {
        onSuccess: () => onClose(),
        onError: () => setCreationError(true),
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
                {selectedProcedure
                  ? t("clinic.procedures.form.create")
                  : t("clinic.procedures.templates.title")}
              </Modal.Heading>
            </Modal.Header>
            <Modal.Body>
              {selectedProcedure ? (
                <>
                  <TextField isRequired value={customName} onChange={setCustomName}>
                    <Label>{t("clinic.procedures.form.nameLabel")}</Label>
                    <Input
                      placeholder={t("clinic.procedures.form.namePlaceholder")}
                    />
                  </TextField>
                  {selectedProcedure.description && (
                    <p className="text-sm text-muted">
                      {selectedProcedure.description}
                    </p>
                  )}
                  {creationError && (
                    <p className="text-sm text-danger">
                      {t("clinic.procedures.form.error")}
                    </p>
                  )}
                </>
              ) : (
                <>
                  {isLoading && (
                    <ClinicLoadingState label={t("common.states.loading")} />
                  )}

                  {error && (
                    <Alert status="danger">
                      <Alert.Indicator />
                      <Alert.Content>
                        <Alert.Title>{t("clinic.procedures.failedToLoad")}</Alert.Title>
                      </Alert.Content>
                    </Alert>
                  )}

                  {!isLoading && !error && (!groups || groups.length === 0) && (
                    <ClinicEmptyState title={t("clinic.procedures.empty")} />
                  )}

                  {!isLoading && !error && groups && groups.length > 0 && (
                    <div className="flex flex-col gap-2">
                      {groups.map((group) => (
                        <Disclosure
                          key={group.project.id}
                          isExpanded={expandedProjectId === group.project.id}
                          onExpandedChange={(expanded) =>
                            setExpandedProjectId(expanded ? group.project.id : null)
                          }
                        >
                          <Disclosure.Heading>
                            <Button
                              slot="trigger"
                              variant="ghost"
                              className="h-auto w-full justify-start gap-3 p-3 text-start"
                            >
                              <FaIcon icon="folder" aria-hidden="true" className="text-muted" />
                              <span className="min-w-0 flex-1">
                                <span className="block font-medium">{group.project.name}</span>
                                <span className="mt-1 block text-sm text-muted">
                                  {t("clinic.procedures.templates.procedureCount", { count: group.procedures.length })}
                                </span>
                              </span>
                              <Disclosure.Indicator className="ms-auto" />
                            </Button>
                          </Disclosure.Heading>
                          <Disclosure.Content>
                            <Disclosure.Body className="flex flex-col gap-1 px-3 pb-3">
                              {group.procedures.length === 0 ? (
                                <p className="text-sm text-muted">
                                  {t("clinic.procedures.templateManagement.noProcedures")}
                                </p>
                              ) : (
                                group.procedures.map((procedure) => (
                                  <Tooltip key={procedure.id}>
                                    <Button
                                      variant="ghost"
                                      className="h-auto w-full justify-start gap-3 p-3 text-start"
                                      onPress={() => handleSelect(procedure)}
                                    >
                                      <FaIcon icon="list-check" aria-hidden="true" className="text-muted" />
                                      <span className="min-w-0 flex-1">
                                        <span className="block font-medium">{procedure.name}</span>
                                        {procedure.description && (
                                          <span className="mt-1 block text-sm text-muted line-clamp-1">
                                            {procedure.description}
                                          </span>
                                        )}
                                      </span>
                                      <Chip size="sm" variant="soft">
                                        {procedure.stats?.events ?? 0}
                                      </Chip>
                                    </Button>
                                    <Tooltip.Content>{procedure.name}</Tooltip.Content>
                                  </Tooltip>
                                ))
                              )}
                            </Disclosure.Body>
                          </Disclosure.Content>
                        </Disclosure>
                      ))}
                    </div>
                  )}
                </>
              )}
            </Modal.Body>
            <Modal.Footer>
              {selectedProcedure ? (
                <>
                  <Button
                    variant="secondary"
                    onPress={() => setSelectedProcedure(null)}
                    isDisabled={createMutation.isPending}
                  >
                    {t("common.actions.cancel")}
                  </Button>
                  <Button
                    variant="primary"
                    onPress={handleCreate}
                    isDisabled={!customName.trim()}
                    isPending={createMutation.isPending}
                  >
                    {t("clinic.procedures.templates.createFrom")}
                  </Button>
                </>
              ) : (
                <Button variant="secondary" onPress={onClose}>
                  {t("common.actions.cancel")}
                </Button>
              )}
            </Modal.Footer>
          </Modal.Dialog>
        </Modal.Container>
      </Modal.Backdrop>
    </Modal>
  );
}

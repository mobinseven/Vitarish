"use client";

import { useCallback, useState } from "react";
import { Button, Label, Input, TextArea, TextField } from "@heroui/react";
import { Widget } from "@heroui-pro/react";
import type { SeaNavigationIntent } from "../../navigation";
import { useCreateSeaProcedure } from "../../hooks/procedures/useCreateSeaProcedure";
import { useUpdateSeaProcedure } from "../../hooks/procedures/useUpdateSeaProcedure";
import { useLocalizedText } from "../../localization";
import type { SeaProcedureReadDto } from "../../models/seaStatModels";

interface SeaProcedureFormProps {
  isOpen: boolean;
  onClose: () => void;
  projectId: string;
  procedure?: SeaProcedureReadDto;
  onNavigate?: (intent: SeaNavigationIntent) => void;
}

export function SeaProcedureForm({ isOpen, onClose, projectId, procedure, onNavigate }: SeaProcedureFormProps) {
  const { t } = useLocalizedText();
  const createMutation = useCreateSeaProcedure();
  const updateMutation = useUpdateSeaProcedure();

  const [name, setName] = useState(procedure?.name ?? "");
  const [description, setDescription] = useState(procedure?.description ?? "");

  const isEditing = !!procedure;
  const isPending = createMutation.isPending || updateMutation.isPending;

  const handleSubmit = useCallback(() => {
    if (!name.trim()) return;

    const data = {
      name: name.trim(),
      description: description.trim() || undefined,
      projectId,
      id: procedure?.id,
    };

    if (isEditing) {
      updateMutation.mutate(data, {
        onSuccess: () => {
          onClose();
          onNavigate?.({ kind: "procedureDetail", projectId, procedureId: procedure!.id });
        },
      });
    } else {
      createMutation.mutate(data, {
        onSuccess: () => {
          onClose();
          onNavigate?.({ kind: "projectDetail", projectId });
        },
      });
    }
  }, [name, description, projectId, isEditing, procedure, createMutation, updateMutation, onClose, onNavigate]);

  if (!isOpen) return null;

  return (
    <Widget>
      <Widget.Header>
        <Widget.Title>{isEditing ? t("sea.procedures.editTitle") : t("sea.procedures.createTitle")}</Widget.Title>
      </Widget.Header>
      <Widget.Content className="flex flex-col gap-4">
        <TextField isRequired value={name} onChange={setName}>
          <Label>{t("sea.procedures.name")}</Label>
          <Input placeholder={t("sea.procedures.namePlaceholder")} />
        </TextField>
        <TextField value={description} onChange={setDescription}>
          <Label>{t("sea.procedures.description")}</Label>
          <TextArea placeholder={t("sea.procedures.descriptionPlaceholder")} />
        </TextField>
        <div className="flex justify-end gap-2">
          <Button variant="ghost" onPress={onClose} isDisabled={isPending}>
            {t("common.actions.cancel")}
          </Button>
          <Button variant="primary" onPress={handleSubmit} isDisabled={isPending || !name.trim()}>
            {isPending ? t("common.states.saving") : t("common.actions.save")}
          </Button>
        </div>
      </Widget.Content>
    </Widget>
  );
}

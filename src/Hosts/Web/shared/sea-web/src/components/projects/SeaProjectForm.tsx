"use client";

import { useCallback, useState } from "react";
import { Button, Label, Input, TextArea, TextField } from "@heroui/react";
import { Widget } from "@heroui-pro/react";
import type { SeaNavigationIntent } from "../../navigation";
import { useCreateSeaProject } from "../../hooks/projects/useCreateSeaProject";
import { useUpdateSeaProject } from "../../hooks/projects/useUpdateSeaProject";
import { useLocalizedText } from "../../localization";
import type { SeaProjectReadDto } from "../../models/seaStatModels";

interface SeaProjectFormProps {
  isOpen: boolean;
  onClose: () => void;
  project?: SeaProjectReadDto;
  onNavigate?: (intent: SeaNavigationIntent) => void;
}

export function SeaProjectForm({ isOpen, onClose, project, onNavigate }: SeaProjectFormProps) {
  const { t } = useLocalizedText();
  const createMutation = useCreateSeaProject();
  const updateMutation = useUpdateSeaProject();

  const [name, setName] = useState(project?.name ?? "");
  const [description, setDescription] = useState(project?.description ?? "");

  const isEditing = !!project;
  const isPending = createMutation.isPending || updateMutation.isPending;

  const handleSubmit = useCallback(() => {
    if (!name.trim()) return;

    const data = {
      name: name.trim(),
      description: description.trim() || undefined,
      template: project?.template ?? false,
      id: project?.id,
    };

    if (isEditing) {
      updateMutation.mutate(data, {
        onSuccess: () => {
          onClose();
          if (project?.id) {
            onNavigate?.({ kind: "projectDetail", projectId: project.id });
          }
        },
      });
    } else {
      createMutation.mutate(data, {
        onSuccess: () => {
          onClose();
          onNavigate?.({ kind: "projectList" });
        },
      });
    }
  }, [name, description, isEditing, project, createMutation, updateMutation, onClose, onNavigate]);

  if (!isOpen) return null;

  return (
    <Widget>
      <Widget.Header>
        <Widget.Title>{isEditing ? t("sea.projects.editTitle") : t("sea.projects.createTitle")}</Widget.Title>
      </Widget.Header>
      <Widget.Content className="flex flex-col gap-4">
        <TextField isRequired value={name} onChange={setName}>
          <Label>{t("sea.projects.name")}</Label>
          <Input placeholder={t("sea.projects.namePlaceholder")} />
        </TextField>
        <TextField value={description} onChange={setDescription}>
          <Label>{t("sea.projects.description")}</Label>
          <TextArea placeholder={t("sea.projects.descriptionPlaceholder")} />
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

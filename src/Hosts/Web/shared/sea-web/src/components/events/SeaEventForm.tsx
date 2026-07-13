"use client";

import { useCallback, useState } from "react";
import { Button, Label, Input, TextArea, TextField } from "@heroui/react";
import { Widget } from "@heroui-pro/react";
import type { SeaNavigationIntent } from "../../navigation";
import { useCreateSeaEvent } from "../../hooks/events/useCreateSeaEvent";
import { useUpdateSeaEvent } from "../../hooks/events/useUpdateSeaEvent";
import { useLocalizedText } from "../../localization";
import type { SeaEventReadDto } from "../../models/seaStatModels";

interface SeaEventFormProps {
  isOpen: boolean;
  onClose: () => void;
  projectId: string;
  procedureId: string;
  previousEventId?: string;
  event?: SeaEventReadDto;
  onNavigate?: (intent: SeaNavigationIntent) => void;
}

export function SeaEventForm({ isOpen, onClose, projectId, procedureId, previousEventId, event, onNavigate }: SeaEventFormProps) {
  const { t } = useLocalizedText();
  const createMutation = useCreateSeaEvent();
  const updateMutation = useUpdateSeaEvent();

  const [title, setTitle] = useState(event?.title ?? "");
  const [description, setDescription] = useState(event?.description ?? "");

  const isEditing = !!event;
  const isPending = createMutation.isPending || updateMutation.isPending;

  const handleSubmit = useCallback(() => {
    if (!title.trim()) return;

    if (isEditing) {
      updateMutation.mutate(
        {
          id: event.id,
          projectId,
          procedureId,
          title: title.trim(),
          description: description.trim() || undefined,
        },
        {
          onSuccess: () => {
            onClose();
            onNavigate?.({ kind: "eventDetail", projectId, procedureId, eventId: event.id });
          },
        }
      );
    } else {
      createMutation.mutate(
        {
          previousEventId,
          data: {
            projectId,
            procedureId,
            title: title.trim(),
            description: description.trim() || undefined,
          },
        },
        {
          onSuccess: () => {
            onClose();
            onNavigate?.({ kind: "procedureDetail", projectId, procedureId });
          },
        }
      );
    }
  }, [title, description, projectId, procedureId, previousEventId, isEditing, event, createMutation, updateMutation, onClose, onNavigate]);

  if (!isOpen) return null;

  return (
    <Widget>
      <Widget.Header>
        <Widget.Title>{isEditing ? t("sea.events.editTitle") : t("sea.events.createTitle")}</Widget.Title>
      </Widget.Header>
      <Widget.Content className="flex flex-col gap-4">
        <TextField isRequired value={title} onChange={setTitle}>
          <Label>{t("sea.events.title_label")}</Label>
          <Input placeholder={t("sea.events.titlePlaceholder")} />
        </TextField>
        <TextField value={description} onChange={setDescription}>
          <Label>{t("sea.events.description")}</Label>
          <TextArea placeholder={t("sea.events.descriptionPlaceholder")} />
        </TextField>
        <div className="flex justify-end gap-2">
          <Button variant="ghost" onPress={onClose} isDisabled={isPending}>
            {t("common.actions.cancel")}
          </Button>
          <Button variant="primary" onPress={handleSubmit} isDisabled={isPending || !title.trim()}>
            {isPending ? t("common.states.saving") : t("common.actions.save")}
          </Button>
        </div>
      </Widget.Content>
    </Widget>
  );
}

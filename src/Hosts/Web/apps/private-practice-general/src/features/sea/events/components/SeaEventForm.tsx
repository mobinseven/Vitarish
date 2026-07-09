"use client";

import { useCallback, useState } from "react";
import { useRouter } from "next/navigation";
import { Button, Label, Input, TextArea, TextField } from "@heroui/react";
import { Widget } from "@heroui-pro/react";
import { Routes } from "@/app/routes";
import { useCreateSeaEvent } from "../hooks/useCreateSeaEvent";
import { useUpdateSeaEvent } from "../hooks/useUpdateSeaEvent";
import { useLocalizedText } from "@/localization";
import type { SeaEventReadDto } from "../../shared/models/seaStatModels";

interface SeaEventFormProps {
  isOpen: boolean;
  onClose: () => void;
  projectId: string;
  procedureId: string;
  previousEventId?: string;
  event?: SeaEventReadDto;
}

export function SeaEventForm({ isOpen, onClose, projectId, procedureId, previousEventId, event }: SeaEventFormProps) {
  const router = useRouter();
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
            router.push(Routes.Sea.Events.Detail(projectId, procedureId, event.id));
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
            router.push(Routes.Sea.Procedures.Detail(projectId, procedureId));
          },
        }
      );
    }
  }, [title, description, projectId, procedureId, previousEventId, isEditing, event, createMutation, updateMutation, onClose, router]);

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

"use client";

import { useEffect, useState } from "react";
import {
  Button,
  Input,
  Label,
  TextArea,
  TextField,
  Tooltip,
} from "@heroui/react";
import { FaIcon } from "@vitarish/web-ui";
import { PressableFeedback } from "@heroui-pro/react";
import {
  useCreateSeaEvent,
  useDeleteSeaEvent,
  useMoveSeaEventDown,
  useMoveSeaEventUp,
  useSeaEventDetail,
  useSeaEvents,
  useUpdateSeaEvent,
} from "@vitarish/sea-web";
import { useLocalizedText } from "../../localization";
import { TemplateConfirmDialog } from "./TemplateConfirmDialog";

export function RoutineStepEditorPane({
  procedureId,
  eventId,
}: {
  procedureId: string | null;
  eventId: string | null;
}) {
  const { t } = useLocalizedText();
  const eventQuery = useSeaEventDetail(eventId ?? "");
  const events = useSeaEvents(procedureId ?? "").data ?? [];
  const event = eventQuery.data;
  const update = useUpdateSeaEvent();
  const create = useCreateSeaEvent();
  const remove = useDeleteSeaEvent();
  const up = useMoveSeaEventUp();
  const down = useMoveSeaEventDown();
  const [title, setTitle] = useState("");
  const [description, setDescription] = useState("");
  const [confirmDelete, setConfirmDelete] = useState(false);
  useEffect(() => {
    setTitle(event?.title ?? "");
    setDescription(event?.description ?? "");
  }, [event]);
  if (!eventId || !event)
    return (
      <p className="p-4 text-sm text-muted">
        {t("clinic.procedures.templateManagement.selectStep")}
      </p>
    );
  const position = events.findIndex((item) => item.id === event.id);
  const busy =
    update.isPending ||
    create.isPending ||
    remove.isPending ||
    up.isPending ||
    down.isPending;
  return (
    <div className="flex flex-col gap-3">
      <TextField value={title} onChange={setTitle} variant="secondary">
        <Label>{t("clinic.procedures.templateManagement.stepTitle")}</Label>
        <Input
          placeholder={t(
            "clinic.procedures.templateManagement.stepTitlePlaceholder",
          )}
        />
      </TextField>
      <TextField
        value={description}
        onChange={setDescription}
        variant="secondary"
      >
        <Label>
          {t("clinic.procedures.templateManagement.stepDescription")}
        </Label>
        <TextArea
          placeholder={t(
            "clinic.procedures.templateManagement.stepDescriptionPlaceholder",
          )}
        />
      </TextField>
      <Tooltip>
        <Button
          isIconOnly
          aria-label={t("clinic.procedures.templateManagement.pending")}
          isDisabled={busy}
          onPress={() => update.mutate({ id: event.id, title, description })}
        >
          <PressableFeedback.Ripple />
          <FaIcon icon="floppy-disk" aria-hidden="true" />
        </Button>
        <Tooltip.Content>
          {t("clinic.procedures.templateManagement.pending")}
        </Tooltip.Content>
      </Tooltip>
      <div className="flex gap-2">
        <Tooltip>
          <Button
            isIconOnly
            aria-label={t("clinic.procedures.templateManagement.moveStepUp")}
            isDisabled={busy || position <= 0}
            onPress={() => up.mutate(event.id)}
          >
            <FaIcon icon="arrow-up" aria-hidden="true" />
          </Button>
          <Tooltip.Content>
            {t("clinic.procedures.templateManagement.moveStepUp")}
          </Tooltip.Content>
        </Tooltip>
        <Tooltip>
          <Button
            isIconOnly
            aria-label={t("clinic.procedures.templateManagement.moveStepDown")}
            isDisabled={busy || position < 0 || position >= events.length - 1}
            onPress={() => down.mutate(event.id)}
          >
            <FaIcon icon="arrow-down" aria-hidden="true" />
          </Button>
          <Tooltip.Content>
            {t("clinic.procedures.templateManagement.moveStepDown")}
          </Tooltip.Content>
        </Tooltip>
      </div>
      <Tooltip>
        <Button
          isIconOnly
          aria-label={t("clinic.procedures.templateManagement.addStep")}
          isDisabled={busy}
          onPress={() =>
            create.mutate({
              previousEventId: event.id,
              data: {
                procedureId: event.procedureId,
                projectId: event.projectId,
                title: t("clinic.procedures.templateManagement.stepTitle"),
                description: "",
              },
            })
          }
        >
          <FaIcon icon="plus" aria-hidden="true" />
        </Button>
        <Tooltip.Content>
          {t("clinic.procedures.templateManagement.addStep")}
        </Tooltip.Content>
      </Tooltip>
      <Tooltip>
        <Button
          isIconOnly
          aria-label={t("clinic.procedures.templateManagement.deleteStep")}
          variant="danger"
          isDisabled={busy}
          onPress={() => setConfirmDelete(true)}
        >
          <FaIcon icon="trash" aria-hidden="true" />
        </Button>
        <Tooltip.Content>
          {t("clinic.procedures.templateManagement.deleteStep")}
        </Tooltip.Content>
      </Tooltip>
      {confirmDelete && (
        <TemplateConfirmDialog
          title={t("clinic.procedures.templateManagement.deleteStep")}
          message={t("clinic.procedures.templateManagement.deleteStepConfirm")}
          isPending={remove.isPending}
          onClose={() => setConfirmDelete(false)}
          onConfirm={() =>
            remove.mutate(event.id, {
              onSuccess: () => setConfirmDelete(false),
            })
          }
        />
      )}
    </div>
  );
}

"use client";

import { useState } from "react";
import { Button, Chip, Label, Link, TextArea, TextField } from "@heroui/react";
import { DropZone, Timeline } from "@heroui-pro/react";
import { toSeaFileAttachment } from "@vitarish/sea-web";
import { EmptyStateSurface } from "@vitarish/web-ui";
import {
  useCreatePatientProcedureEventItemFile,
  useCreatePatientProcedureEventItemText,
  useDeletePatientProcedureEventItem,
} from "../../../hooks/patients/procedures/usePatientProcedureEventItemMutations";
import { usePatientProcedureEventItems } from "../../../hooks/patients/procedures/usePatientProcedureEventItems";
import { formatDateTime, formatFileSize, useLocalizedText } from "../../../localization";
import type { EventItemSummary } from "../../../models/patients/procedures/PatientProcedureEventItemModels";

interface PatientProcedureEventItemsPanelProps {
  eventId: string;
  canWrite: boolean;
}

function getItemMessageKey(item: EventItemSummary) {
  if (item.type === "media") return "clinic.procedures.items.media";
  if (item.type === "file") return "clinic.procedures.items.file";
  return "clinic.procedures.items.note";
}

export function PatientProcedureEventItemsPanel({
  eventId,
  canWrite,
}: PatientProcedureEventItemsPanelProps) {
  const { t, locale } = useLocalizedText();
  const query = usePatientProcedureEventItems(eventId);
  const createNote = useCreatePatientProcedureEventItemText();
  const createFile = useCreatePatientProcedureEventItemFile();
  const remove = useDeletePatientProcedureEventItem();
  const [note, setNote] = useState("");
  const [file, setFile] = useState<File | null>(null);
  const [fileFailed, setFileFailed] = useState(false);

  const submitNote = async () => {
    const content = note.trim();
    if (!content) return;

    try {
      await createNote.mutateAsync({
        eventId,
        content,
        contentType: "text/plain",
        size: new Blob([content]).size,
      });
      setNote("");
    } catch {
      // The mutation state renders the recoverable failure and the draft stays intact.
    }
  };

  const submitFile = async () => {
    if (!file) return;

    setFileFailed(false);
    try {
      const attachment = await toSeaFileAttachment(eventId, file);
      await createFile.mutateAsync({ ...attachment, fileName: file.name });
      setFile(null);
    } catch {
      setFileFailed(true);
    }
  };

  const items = [...(query.data ?? [])].sort((left, right) =>
    right.createdAt.localeCompare(left.createdAt),
  );

  return (
    <section className="flex flex-col gap-4" aria-labelledby="clinical-evidence-title">
      <h3 id="clinical-evidence-title" className="text-lg font-semibold">
        {t("clinic.procedures.items.title")}
      </h3>

      {query.isLoading ? (
        <p>{t("common.states.loading")}</p>
      ) : query.error ? (
        <p className="text-danger" role="alert">
          {t("clinic.procedures.items.loadFailed")}
        </p>
      ) : items.length === 0 ? (
        <EmptyStateSurface title={t("clinic.procedures.items.empty")} />
      ) : (
        <Timeline
          aria-label={t("clinic.procedures.items.title")}
          density="compact"
          size="sm"
        >
          {items.map((item) => {
            const fileName = item.fileName ?? t("clinic.procedures.items.genericAttachment");
            const fileMeta = [
              item.contentType,
              item.size == null ? null : formatFileSize(item.size, locale),
            ]
              .filter(Boolean)
              .join(" · ");

            return (
              <Timeline.Item key={item.id} align="start" id={item.id} status="default">
                <Timeline.Marker aria-hidden="true" />
                <Timeline.Content className="gap-2">
                  <div className="flex min-w-0 flex-col gap-1 sm:flex-row sm:items-center sm:justify-between sm:gap-3">
                    <Chip className="self-start" size="sm" variant="soft">
                      {t(getItemMessageKey(item))}
                    </Chip>
                    <time
                      className="text-muted shrink-0 text-xs leading-5"
                      dateTime={item.createdAt}
                    >
                      {formatDateTime(item.createdAt, locale)}
                    </time>
                  </div>

                  {item.type === "note" ? (
                    <p className="text-foreground m-0 whitespace-pre-wrap text-sm leading-relaxed">
                      {item.content}
                    </p>
                  ) : (
                    <div className="flex min-w-0 flex-col items-start gap-1 text-sm">
                      <Link download={fileName} href={item.content}>
                        {fileName}
                      </Link>
                      {fileMeta ? <span className="text-muted text-xs">{fileMeta}</span> : null}
                    </div>
                  )}

                  {canWrite ? (
                    <Button
                      className="self-start"
                      isPending={remove.isPending}
                      size="sm"
                      variant="danger-soft"
                      onPress={() => remove.mutate(item.id)}
                    >
                      {t("clinic.procedures.items.delete")}
                    </Button>
                  ) : null}
                </Timeline.Content>
              </Timeline.Item>
            );
          })}
        </Timeline>
      )}

      {remove.isError ? (
        <p className="text-danger text-sm" role="alert">
          {t("clinic.procedures.items.deleteFailed")}
        </p>
      ) : null}

      {canWrite ? (
        <div className="grid gap-4 md:grid-cols-2">
          <form
            className="flex flex-col gap-3"
            onSubmit={(submitEvent) => {
              submitEvent.preventDefault();
              void submitNote();
            }}
          >
            <TextField fullWidth value={note} onChange={setNote}>
              <Label>{t("clinic.procedures.items.noteLabel")}</Label>
              <TextArea rows={4} variant="secondary" />
            </TextField>
            <Button
              className="self-start"
              isDisabled={!note.trim()}
              isPending={createNote.isPending}
              type="submit"
            >
              {t("clinic.procedures.items.addNote")}
            </Button>
            {createNote.isError ? (
              <p className="text-danger text-sm" role="alert">
                {t("clinic.procedures.items.saveFailed")}
              </p>
            ) : null}
          </form>

          <DropZone>
            <DropZone.Area isDisabled={createFile.isPending}>
              <DropZone.Label>{t("clinic.procedures.items.chooseFile")}</DropZone.Label>
              <DropZone.Description>{t("clinic.procedures.items.oneFile")}</DropZone.Description>
              <DropZone.Trigger isDisabled={createFile.isPending}>
                {t("clinic.procedures.items.browse")}
              </DropZone.Trigger>
            </DropZone.Area>
            <DropZone.Input
              multiple={false}
              onSelect={(files) => {
                setFile(files.item(0));
                setFileFailed(false);
              }}
            />

            {file ? (
              <DropZone.FileList>
                <DropZone.FileItem
                  status={createFile.isPending ? "uploading" : fileFailed ? "failed" : undefined}
                >
                  <DropZone.FileInfo>
                    <DropZone.FileName>{file.name}</DropZone.FileName>
                    <DropZone.FileMeta>
                      {file.type || "application/octet-stream"} · {formatFileSize(file.size, locale)}
                    </DropZone.FileMeta>
                    <Button
                      className="mt-2 self-start"
                      isPending={createFile.isPending}
                      size="sm"
                      variant={fileFailed ? "danger-soft" : "primary"}
                      onPress={() => void submitFile()}
                    >
                      {fileFailed
                        ? t("clinic.procedures.items.retry")
                        : t("clinic.procedures.items.upload")}
                    </Button>
                  </DropZone.FileInfo>
                  <DropZone.FileRemoveTrigger
                    aria-label={t("common.actions.remove")}
                    onPress={() => {
                      setFile(null);
                      setFileFailed(false);
                    }}
                  />
                </DropZone.FileItem>
              </DropZone.FileList>
            ) : null}
          </DropZone>
        </div>
      ) : null}
    </section>
  );
}

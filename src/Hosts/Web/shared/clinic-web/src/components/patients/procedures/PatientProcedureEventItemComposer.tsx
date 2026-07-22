"use client";

import { useEffect, useRef, useState } from "react";
import {
  ChatAttachment,
  ChatAttachmentGroup,
  ChatAttachmentInput,
  PromptInput,
} from "@heroui-pro/react";
import { FaIcon } from "@vitarish/web-ui";
import {
  appendConfirmedTranscript,
  LiveDictation,
  useLiveDictation,
} from "@vitarish/intelligence-web";
import {
  useCreatePatientProcedureEventItemFile,
  useCreatePatientProcedureEventItemText,
} from "../../../hooks/patients/procedures/usePatientProcedureEventItemMutations";
import { useLocalizedText } from "../../../localization";

interface PatientProcedureEventItemComposerProps {
  eventId: string;
}

export function PatientProcedureEventItemComposer({
  eventId,
}: PatientProcedureEventItemComposerProps) {
  const { t, locale } = useLocalizedText();
  const createNote = useCreatePatientProcedureEventItemText();
  const createFile = useCreatePatientProcedureEventItemFile();
  const [note, setNote] = useState("");
  const [file, setFile] = useState<File | null>(null);
  const fileRef = useRef<File | null>(null);
  const [previewUrl, setPreviewUrl] = useState<string>();
  const [fileFailed, setFileFailed] = useState(false);
  const dictation = useLiveDictation({
    enabled: true,
    onSegment: (segment) =>
      setNote((current) => appendConfirmedTranscript(current, segment)),
    preferredLanguage: locale === "fa" ? "fa-IR" : "en-US",
    sessionKey: eventId,
  });

  useEffect(
    () => () => {
      if (previewUrl) URL.revokeObjectURL(previewUrl);
    },
    [previewUrl],
  );

  const selectFile = (nextFile?: File) => {
    fileRef.current = nextFile ?? null;
    setFile(nextFile ?? null);
    setFileFailed(false);
    createFile.reset();
    setPreviewUrl(
      nextFile &&
        (nextFile.type.startsWith("image/") ||
          nextFile.type.startsWith("video/"))
        ? URL.createObjectURL(nextFile)
        : undefined,
    );
  };

  const submit = async () => {
    if (dictation.isActive) return;
    const content = note.trim();
    const selectedFile = file;
    if (!content && !selectedFile) return;

    if (content) {
      try {
        await createNote.mutateAsync({
          eventId,
          content,
          contentType: "text/plain",
          size: new Blob([content]).size,
        });
        setNote((current) =>
          current.startsWith(content) ? current.slice(content.length) : current,
        );
      } catch {
        // The mutation state renders the localized failure and retains the draft.
      }
    }

    if (selectedFile) {
      setFileFailed(false);
      try {
        await createFile.mutateAsync({ eventId, file: selectedFile });
        if (fileRef.current === selectedFile) {
          fileRef.current = null;
          setFile(null);
          setPreviewUrl(undefined);
        }
      } catch {
        setFileFailed(true);
      }
    }
  };

  const isSubmitting = createNote.isPending || createFile.isPending;
  return (
    <div className="shrink-0 pt-3">
      <PromptInput
        className="mx-auto w-full max-w-4xl"
        status={isSubmitting ? "submitted" : "ready"}
        value={note}
        variant="secondary"
        onSubmit={() => void submit()}
        onValueChange={setNote}
      >
        <ChatAttachmentInput
          multiple={false}
          onFilesSelected={(files) => selectFile(files?.[0])}
        >
          <ChatAttachmentInput.Dropzone
            render={(dropzoneProps) => (
              <PromptInput.Shell {...dropzoneProps}>
                <PromptInput.Content>
                  {file ? (
                    <PromptInput.Attachments>
                      <ChatAttachmentGroup>
                        <ChatAttachment
                          mimeType={file.type}
                          name={file.name}
                          src={previewUrl}
                        >
                          <ChatAttachment.Preview />
                          <ChatAttachment.Remove
                            aria-label={t("common.actions.remove")}
                            onPress={() => selectFile()}
                          />
                        </ChatAttachment>
                      </ChatAttachmentGroup>
                    </PromptInput.Attachments>
                  ) : null}
                  <PromptInput.TextArea
                    aria-label={t("clinic.procedures.items.noteLabel")}
                    placeholder={t(
                      "clinic.procedures.items.composerPlaceholder",
                    )}
                  />
                </PromptInput.Content>
                <PromptInput.Toolbar>
                  <PromptInput.ToolbarStart>
                    <ChatAttachmentInput.Trigger
                      render={(triggerProps) => (
                        <PromptInput.Action
                          {...triggerProps}
                          aria-label={t("clinic.procedures.items.chooseFile")}
                          tooltip={t("clinic.procedures.items.chooseFile")}
                        >
                          <FaIcon
                            icon="paperclip"
                            aria-hidden="true"
                            className="size-4"
                          />
                        </PromptInput.Action>
                      )}
                    />
                    <LiveDictation controller={dictation} />
                  </PromptInput.ToolbarStart>
                  <PromptInput.ToolbarEnd>
                    <PromptInput.Send
                      aria-label={t("clinic.procedures.items.send")}
                      isDisabled={
                        isSubmitting ||
                        dictation.isActive ||
                        (!note.trim() && !file)
                      }
                    />
                  </PromptInput.ToolbarEnd>
                </PromptInput.Toolbar>
              </PromptInput.Shell>
            )}
          />
        </ChatAttachmentInput>
        <PromptInput.Footer>
          {t("clinic.procedures.items.oneFile")}
        </PromptInput.Footer>
      </PromptInput>
      {createNote.isError ? (
        <p
          className="text-danger mx-auto mt-2 w-full max-w-4xl text-sm"
          role="alert"
        >
          {t("clinic.procedures.items.saveFailed")}
        </p>
      ) : null}
      {createFile.isError || fileFailed ? (
        <p
          className="text-danger mx-auto mt-2 w-full max-w-4xl text-sm"
          role="alert"
        >
          {t("clinic.procedures.items.uploadFailed")}
        </p>
      ) : null}
    </div>
  );
}

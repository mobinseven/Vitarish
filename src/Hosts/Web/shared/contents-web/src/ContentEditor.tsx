"use client";
import { useEffect, useRef, useState } from "react";
import { Button, Tooltip } from "@heroui/react";
import {
  useArchiveContent,
  useContentItem,
  useContentRevisions,
  useSaveContent,
} from "./useContents";
import type {
  ContentItemInput,
  ContentItemType,
  ContentStatus,
  ContentsClient,
} from "./types";
import {
  blankContentDraft,
  contentDraftFromItem,
  updatePrimaryBodyText,
} from "./contentDraft";

export interface ContentEditorText {
  createTitle: string;
  editTitle: string;
  title: string;
  slug: string;
  path: string;
  excerpt: string;
  body: string;
  locale: string;
  type: string;
  status: string;
  typeValue: (value: ContentItemType) => string;
  statusValue: (value: ContentStatus) => string;
  save: string;
  archive: string;
  cancel: string;
  revisions: string;
  revision: (version: number, date: string) => string;
  required: string;
  loading: string;
  failed: string;
  saveFailed: string;
}

interface ContentEditorProps {
  client: ContentsClient;
  contentId?: string;
  text: ContentEditorText;
  formatDate: (value: string) => string;
  onDone: () => void;
  onCancel: () => void;
}

export function ContentEditor({
  client,
  contentId,
  text,
  formatDate,
  onDone,
  onCancel,
}: ContentEditorProps) {
  const item = useContentItem(client, contentId);
  const revisions = useContentRevisions(client, contentId);
  const save = useSaveContent(client, contentId);
  const archive = useArchiveContent(client);
  const initializedContentId = useRef<string | undefined>(undefined);
  const [form, setForm] = useState(blankContentDraft);
  const [attempted, setAttempted] = useState(false);

  useEffect(() => {
    if (item.data && initializedContentId.current !== item.data.id) {
      setForm(contentDraftFromItem(item.data));
      initializedContentId.current = item.data.id;
    }
  }, [item.data]);
  if (contentId && item.isLoading) return <p role="status">{text.loading}</p>;
  if (contentId && item.isError) return <p role="alert">{text.failed}</p>;
  const valid = Boolean(
    form.title.trim() &&
    form.slug.trim() &&
    form.canonicalPath.trim() &&
    form.locale.trim(),
  );
  const set = <K extends keyof ContentItemInput>(
    key: K,
    value: ContentItemInput[K],
  ) => setForm((current) => ({ ...current, [key]: value }));
  const submit = () => {
    setAttempted(true);
    if (!valid) return;
    save.mutate(form, { onSuccess: onDone });
  };
  const updateBody = (textValue: string) =>
    set("body", updatePrimaryBodyText(form.body, textValue));
  return (
    <section
      className="flex flex-col gap-6"
      aria-labelledby="content-editor-title"
    >
      <h1 id="content-editor-title" className="text-2xl font-semibold">
        {contentId ? text.editTitle : text.createTitle}
      </h1>
      <div className="grid gap-4 md:grid-cols-2">
        {(
          [
            ["title", text.title],
            ["slug", text.slug],
            ["canonicalPath", text.path],
            ["locale", text.locale],
          ] as const
        ).map(([key, label]) => (
          <label key={key} className="grid gap-1 text-sm">
            <span>{label}</span>
            <input
              className="rounded-lg border border-default-300 bg-transparent px-3 py-2"
              value={form[key]}
              onChange={(e) => set(key, e.target.value)}
              aria-invalid={attempted && !form[key].trim()}
            />
            {attempted && !form[key].trim() && (
              <span className="text-danger">{text.required}</span>
            )}
          </label>
        ))}
        <label className="grid gap-1 text-sm">
          <span>{text.type}</span>
          <select
            className="rounded-lg border border-default-300 bg-transparent px-3 py-2"
            value={form.type}
            onChange={(e) => set("type", e.target.value as ContentItemType)}
          >
            {(
              [
                "Article",
                "BlogPost",
                "Announcement",
                "Page",
                "Guide",
              ] as ContentItemType[]
            ).map((value) => (
              <option key={value} value={value}>
                {text.typeValue(value)}
              </option>
            ))}
          </select>
        </label>
        <label className="grid gap-1 text-sm">
          <span>{text.status}</span>
          <select
            className="rounded-lg border border-default-300 bg-transparent px-3 py-2"
            value={form.status}
            onChange={(e) => set("status", e.target.value as ContentStatus)}
          >
            {(["Draft", "Published"] as ContentStatus[]).map((value) => (
              <option key={value} value={value}>
                {text.statusValue(value)}
              </option>
            ))}
          </select>
        </label>
      </div>
      <label className="grid gap-1 text-sm">
        <span>{text.excerpt}</span>
        <textarea
          className="min-h-20 rounded-lg border border-default-300 bg-transparent px-3 py-2"
          value={form.excerpt}
          onChange={(e) => set("excerpt", e.target.value)}
        />
      </label>
      <label className="grid gap-1 text-sm">
        <span>{text.body}</span>
        <textarea
          className="min-h-64 rounded-lg border border-default-300 bg-transparent px-3 py-2"
          value={form.body[0]?.text ?? ""}
          onChange={(e) => updateBody(e.target.value)}
        />
      </label>
      {(save.isError || archive.isError) && (
        <p role="alert">{text.saveFailed}</p>
      )}
      <div className="flex flex-wrap gap-2">
        <Tooltip>
          <Button
            onPress={submit}
            isPending={save.isPending}
            aria-label={text.save}
          >
            <i className="fa-light fa-floppy-disk" aria-hidden="true" />
            <span>{text.save}</span>
          </Button>
          <Tooltip.Content arrowBoundaryOffset={0}>{text.save}</Tooltip.Content>
        </Tooltip>
        <Tooltip>
          <Button
            variant="secondary"
            onPress={onCancel}
            aria-label={text.cancel}
          >
            <i className="fa-light fa-xmark" aria-hidden="true" />
            <span>{text.cancel}</span>
          </Button>
          <Tooltip.Content arrowBoundaryOffset={0}>{text.cancel}</Tooltip.Content>
        </Tooltip>
        {contentId && form.status !== "Archived" && (
          <Tooltip>
            <Button
              variant="danger"
              onPress={() => archive.mutate(contentId, { onSuccess: onDone })}
              aria-label={text.archive}
            >
              <i className="fa-light fa-box-archive" aria-hidden="true" />
              <span>{text.archive}</span>
            </Button>
            <Tooltip.Content arrowBoundaryOffset={0}>{text.archive}</Tooltip.Content>
          </Tooltip>
        )}
      </div>
      {contentId && (
        <aside aria-labelledby="content-revisions-title">
          <h2 id="content-revisions-title" className="font-semibold">
            {text.revisions}
          </h2>
          <ul>
            {revisions.data?.map((revision) => (
              <li key={revision.id}>
                {text.revision(
                  revision.version,
                  formatDate(revision.createdAt),
                )}
              </li>
            ))}
          </ul>
        </aside>
      )}
    </section>
  );
}

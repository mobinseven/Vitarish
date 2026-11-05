"use client";

import { Surface } from "@heroui/react";
import { ApiError } from "@vitarish/api-client";
import { formatDateTime, useLocalizedText } from "@vitarish/localization";
import { useState } from "react";
import { contentEntryStatuses, type ContentEntry, type ContentEntryFields, type ContentSite, type ContentType } from "./contentsStudioApi";
import { ContentFieldEditor, updateFieldValue } from "./ContentFieldEditor";
import { StudioButton, StudioFailure, StudioInput, StudioSelect, StudioState, StudioTextArea } from "./StudioControls";
import { useArchiveEntry, useContentEntries, useContentEntry, useContentRevisions, useCreateContentEntry, usePublishEntry, useRestoreRevision, useSaveDraft, useUnpublishEntry } from "./useContentsStudio";

type EntryDraft = { slug: string; canonicalPath: string; title: string; excerpt: string; fields: Record<string, unknown> };
const emptyDraft: EntryDraft = { slug: "", canonicalPath: "", title: "", excerpt: "", fields: {} };
const pageSize = 20;
const objectValue = (value: unknown): Record<string, unknown> => value !== null && typeof value === "object" && !Array.isArray(value) ? value as Record<string, unknown> : {};

function EntryForm({ entry, site, type, entries, onDone, onReload }: { entry?: ContentEntry; site: ContentSite; type: ContentType; entries: ContentEntry[]; onDone: () => void; onReload?: () => void }) {
  const { t, locale: displayLocale } = useLocalizedText();
  const [draft, setDraft] = useState<EntryDraft>(() => entry ? { slug: entry.slug, canonicalPath: entry.canonicalPath, title: entry.title, excerpt: entry.excerpt ?? "", fields: objectValue(entry.fields) } : emptyDraft);
  const [locale, setLocale] = useState(site.defaultLocale);
  const [failure, setFailure] = useState<unknown>();
  const create = useCreateContentEntry();
  const save = useSaveDraft();
  const publish = usePublishEntry();
  const unpublish = useUnpublishEntry();
  const archive = useArchiveEntry();
  const restore = useRestoreRevision();
  const pending = create.isPending || save.isPending || publish.isPending || unpublish.isPending || archive.isPending || restore.isPending;
  const revisions = useContentRevisions(entry?.id);
  const problemErrors = failure instanceof ApiError ? failure.problem.errors ?? {} : {};
  const errors = Object.fromEntries(Object.keys(problemErrors).map((key) => [key, [t("contentsStudio.validation")]]));
  const fieldError = (key: string) => (errors[key] ?? errors[key[0].toUpperCase() + key.slice(1)])?.join(" ");
  const update = <K extends keyof EntryDraft>(key: K, value: EntryDraft[K]) => setDraft((current) => ({ ...current, [key]: value }));
  const callbacks = { onSuccess: onDone, onError: (error: unknown) => setFailure(error) };
  const saveCurrent = () => {
    setFailure(undefined);
    const input = { ...draft, fields: draft.fields as ContentEntryFields };
    if (entry) save.mutate({ id: entry.id, input: { ...input, expectedVersion: entry.version } }, callbacks);
    else create.mutate({ ...input, siteId: site.id, contentTypeId: type.id }, callbacks);
  };
  return <Surface className="flex flex-col gap-5 rounded-3xl p-6">
    <h2 className="text-lg font-semibold">{entry ? t("contentsStudio.edit") : t("contentsStudio.newEntry")}</h2>
    <StudioInput label={t("contentsStudio.slug")} value={draft.slug} onChange={(v) => update("slug", v)} error={fieldError("slug")} />
    <StudioInput label={t("contentsStudio.path")} value={draft.canonicalPath} onChange={(v) => update("canonicalPath", v)} error={fieldError("canonicalPath")} />
    <StudioInput label={t("contentsStudio.entryTitle")} value={draft.title} onChange={(v) => update("title", v)} error={fieldError("title")} />
    <StudioTextArea label={t("contentsStudio.excerpt")} value={draft.excerpt} onChange={(v) => update("excerpt", v)} error={fieldError("excerpt")} />
    {type.fields.map((field) => <ContentFieldEditor key={field.key} definition={field} value={draft.fields[field.key]} onChange={(next) => update("fields", updateFieldValue(draft.fields, field.key, next))} locales={[site.defaultLocale, ...site.locales.filter((item) => item !== site.defaultLocale)]} locale={locale} onLocaleChange={setLocale} entries={entries} path={`fields.${field.key}`} errors={errors} />)}
    {fieldError("fields") && <p role="alert">{fieldError("fields")}</p>}
    {Boolean(failure) && <StudioFailure error={failure} conflict />}
    <div className="flex flex-wrap gap-3"><StudioButton label={t("contentsStudio.saveDraft")} icon="fa-floppy-disk" disabled={pending} onPress={saveCurrent} />
      {entry && <><StudioButton label={t("contentsStudio.publish")} icon="fa-cloud-arrow-up" disabled={pending || !entry.workingRevisionId} onPress={() => entry.workingRevisionId && publish.mutate({ id: entry.id, revisionId: entry.workingRevisionId, expectedVersion: entry.version }, callbacks)} /><StudioButton label={t("contentsStudio.unpublish")} icon="fa-eye-slash" disabled={pending} onPress={() => unpublish.mutate({ id: entry.id, expectedVersion: entry.version }, callbacks)} /><StudioButton label={t("contentsStudio.archive")} icon="fa-box-archive" disabled={pending} onPress={() => archive.mutate({ id: entry.id, expectedVersion: entry.version }, callbacks)} /><StudioButton label={t("contentsStudio.reload")} icon="fa-rotate" disabled={pending} onPress={() => onReload?.()} /></>}
    </div>
    {entry && <div className="flex flex-col gap-3"><h3 className="text-lg font-semibold">{t("contentsStudio.revisions")}</h3>{revisions.isLoading && <StudioState>{t("contentsStudio.loading")}</StudioState>}{revisions.isError && <StudioFailure error={revisions.error} />}{revisions.data?.map((revision) => <div key={revision.id} className="flex flex-wrap items-center gap-3"><span>{t("contentsStudio.revision", { version: revision.version })} · {formatDateTime(revision.createdAt, displayLocale)}</span><StudioButton label={t("contentsStudio.restore")} icon="fa-rotate-left" disabled={pending} onPress={() => restore.mutate({ id: entry.id, revisionId: revision.id, expectedVersion: entry.version }, callbacks)} /></div>)}</div>}
  </Surface>;
}

function LoadedEntry({ id, sites, types, entries, onDone }: { id: string; sites: ContentSite[]; types: ContentType[]; entries: ContentEntry[]; onDone: () => void }) {
  const { t } = useLocalizedText();
  const [generation, setGeneration] = useState(0);
  const entry = useContentEntry(id);
  if (entry.isLoading) return <StudioState>{t("contentsStudio.loading")}</StudioState>;
  if (entry.isError || !entry.data) return <StudioFailure error={entry.error} />;
  const site = sites.find((item) => item.id === entry.data.siteId);
  const type = types.find((item) => item.id === entry.data.contentTypeId);
  if (!site || !type) return <StudioState error>{t("contentsStudio.error")}</StudioState>;
  return <EntryForm key={generation} entry={entry.data} site={site} type={type} entries={entries} onDone={onDone} onReload={() => { void entry.refetch().then((result) => { if (result.isSuccess) setGeneration((n) => n + 1); }); }} />;
}

export function EntriesPanel({ sites, types }: { sites: ContentSite[]; types: ContentType[] }) {
  const { t } = useLocalizedText();
  const [siteId, setSiteId] = useState(sites[0]?.id ?? "");
  const [typeId, setTypeId] = useState("");
  const [status, setStatus] = useState("");
  const [selected, setSelected] = useState<string | null>(null);
  const [generation, setGeneration] = useState(0);
  const [page, setPage] = useState(0);
  const [referencePage, setReferencePage] = useState(0);
  const entries = useContentEntries({ siteId, typeId: typeId || undefined, status: status || undefined, skip: page * pageSize, take: pageSize });
  const referenceEntries = useContentEntries({ siteId, skip: referencePage * pageSize, take: pageSize });
  const site = sites.find((item) => item.id === siteId);
  const type = types.find((item) => item.id === typeId);
  const done = () => { setSelected(null); setGeneration((n) => n + 1); void entries.refetch(); };
  return <div className="flex flex-col gap-6"><Surface className="flex flex-col gap-4 rounded-3xl p-6"><h2 className="text-lg font-semibold">{t("contentsStudio.entries")}</h2>
    <StudioSelect label={t("contentsStudio.site")} value={siteId} onChange={(id) => { setSiteId(id); setSelected(null); setPage(0); setReferencePage(0); }} options={sites.map((item) => ({ id: item.id, label: item.name }))} />
    <StudioSelect label={t("contentsStudio.type")} value={typeId} onChange={(id) => { setTypeId(id); setPage(0); }} options={[{ id: "", label: t("contentsStudio.all") }, ...types.map((item) => ({ id: item.id, label: item.name }))]} />
    <StudioSelect label={t("contentsStudio.status")} value={status} onChange={(id) => { setStatus(id); setPage(0); }} options={["", ...contentEntryStatuses].map((id) => ({ id, label: t(`contentsStudio.${id || "all"}`) }))} />
    <StudioButton label={t("contentsStudio.newEntry")} icon="fa-plus" disabled={!site || !type} onPress={() => setSelected("new")} />
    {entries.isLoading && <StudioState>{t("contentsStudio.loading")}</StudioState>}{entries.isError && <StudioFailure error={entries.error} />}{entries.data?.length === 0 && <StudioState>{t("contentsStudio.empty")}</StudioState>}
    {entries.data?.map((entry) => <div key={entry.id} className="flex flex-wrap items-center gap-3"><span>{entry.title} · {t(`contentsStudio.${entry.status}`)}</span><StudioButton label={t("contentsStudio.edit")} icon="fa-pen" onPress={() => setSelected(entry.id)} /></div>)}
    <div className="flex flex-wrap items-center gap-3"><StudioButton label={t("contentsStudio.previous")} icon="fa-list" disabled={page === 0} onPress={() => setPage((n) => n - 1)} /><span>{t("contentsStudio.page", { page: page + 1 })}</span><StudioButton label={t("contentsStudio.next")} icon="fa-list" disabled={(entries.data?.length ?? 0) < pageSize} onPress={() => setPage((n) => n + 1)} /></div>
  </Surface>{selected && <div className="flex flex-wrap items-center gap-3"><StudioButton label={t("contentsStudio.previous")} icon="fa-list" disabled={referencePage === 0} onPress={() => setReferencePage((n) => n - 1)} /><span>{t("contentsStudio.referencePage", { page: referencePage + 1 })}</span><StudioButton label={t("contentsStudio.next")} icon="fa-list" disabled={(referenceEntries.data?.length ?? 0) < pageSize} onPress={() => setReferencePage((n) => n + 1)} />{referenceEntries.isError && <StudioFailure error={referenceEntries.error} />}</div>}{selected === "new" && site && type && <EntryForm key={`new-${generation}`} site={site} type={type} entries={referenceEntries.data ?? []} onDone={done} />}{selected && selected !== "new" && <LoadedEntry key={`${selected}-${generation}`} id={selected} sites={sites} types={types} entries={referenceEntries.data ?? []} onDone={done} />}</div>;
}

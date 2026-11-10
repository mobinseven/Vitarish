"use client";

import { Alert, Drawer, Surface } from "@heroui/react";
import { ApiError } from "@vitarish/api-client";
import { formatDateTime, useLocalizedText } from "@vitarish/localization";
import { HeaderIcon } from "@vitarish/ui";
import { useState } from "react";
import type { ContentEntry, ContentEntryFields, ContentSite, ContentType } from "./contentsStudioApi";
import styles from "./content-studio.module.css";
import { ContentFieldEditor, updateFieldValue } from "./ContentFieldEditor";
import { EntryStatusChip, isConflict, renderSection, StudioButton, StudioFailure, StudioInput, StudioSelect, StudioState, StudioTextArea } from "./StudioControls";
import { useArchiveEntry, useContentEntry, useContentRevisions, useCreateContentEntry, usePublishEntry, useRestoreRevision, useSaveDraft, useUnpublishEntry } from "./useContentsStudio";

type EntryDraft = { readonly expectedVersion: number | null; slug: string; canonicalPath: string; title: string; excerpt: string; fields: Record<string, unknown> };
const emptyDraft: EntryDraft = { expectedVersion: null, slug: "", canonicalPath: "", title: "", excerpt: "", fields: {} };
const objectValue = (value: unknown): Record<string, unknown> => value !== null && typeof value === "object" && !Array.isArray(value) ? value as Record<string, unknown> : {};

function siteLocales(site: ContentSite): string[] {
  return [site.defaultLocale, ...site.locales.filter((item) => item !== site.defaultLocale)];
}

function RevisionHistory({ entry, expectedVersion, pending, isOpen, onOpenChange, onRestore }: { entry: ContentEntry; expectedVersion: number | null; pending: boolean; isOpen: boolean; onOpenChange: (open: boolean) => void; onRestore: (revisionId: string) => void }) {
  const { t, locale } = useLocalizedText();
  const revisions = useContentRevisions(isOpen ? entry.id : undefined);
  return <Drawer>
    <Drawer.Backdrop isOpen={isOpen} onOpenChange={onOpenChange}>
      <Drawer.Content placement="right" className={styles.studio}>
        <Drawer.Dialog>
          <Drawer.CloseTrigger />
          <Drawer.Header><Drawer.Heading className="text-lg font-semibold">{t("contentsStudio.revisions")}</Drawer.Heading></Drawer.Header>
          <Drawer.Body>
            {revisions.isLoading && <StudioState>{t("contentsStudio.loading")}</StudioState>}
            {revisions.isError && <StudioFailure error={revisions.error} />}
            {revisions.data?.length === 0 && <StudioState>{t("contentsStudio.empty")}</StudioState>}
            <ol className="flex flex-col gap-3">
              {revisions.data?.map((revision) => {
                const current = revision.id === entry.workingRevisionId;
                const published = revision.id === entry.publishedRevisionId;
                return <li key={revision.id}><Surface variant="secondary" className="flex flex-col gap-2 rounded-2xl p-4">
                  <span className="text-base font-semibold">{t("contentsStudio.revision", { version: revision.version })}</span>
                  <span className="text-base text-muted">{formatDateTime(revision.createdAt, locale)}</span>
                  {(current || published) && <span className="text-base">{[current && t("contentsStudio.currentRevision"), published && t("contentsStudio.Published")].filter(Boolean).join(" · ")}</span>}
                  {!current && <div><StudioButton label={t("contentsStudio.restore")} icon="fa-rotate-left" variant="tertiary" disabled={pending || expectedVersion === null} onPress={() => onRestore(revision.id)} /></div>}
                </Surface></li>;
              })}
            </ol>
          </Drawer.Body>
        </Drawer.Dialog>
      </Drawer.Content>
    </Drawer.Backdrop>
  </Drawer>;
}

function EntryForm({ entry, site, type, onBack, onSaved, onReload }: { entry?: ContentEntry; site: ContentSite; type: ContentType; onBack: () => void; onSaved: (id: string) => void; onReload?: () => void }) {
  const { t } = useLocalizedText();
  const [draft, setDraft] = useState<EntryDraft>(() => entry ? { expectedVersion: entry.version, slug: entry.slug, canonicalPath: entry.canonicalPath, title: entry.title, excerpt: entry.excerpt ?? "", fields: objectValue(entry.fields) } : emptyDraft);
  const [locale, setLocale] = useState(site.defaultLocale);
  const [failure, setFailure] = useState<unknown>();
  const [historyOpen, setHistoryOpen] = useState(false);
  const create = useCreateContentEntry();
  const save = useSaveDraft();
  const publish = usePublishEntry();
  const unpublish = useUnpublishEntry();
  const archive = useArchiveEntry();
  const restore = useRestoreRevision();
  const expectedVersion = draft.expectedVersion;
  const pending = create.isPending || save.isPending || publish.isPending || unpublish.isPending || archive.isPending || restore.isPending;
  const problemErrors = failure instanceof ApiError ? failure.problem.errors ?? {} : {};
  const errors = Object.fromEntries(Object.keys(problemErrors).map((key) => [key, [t("contentsStudio.validation")]]));
  const fieldError = (key: string) => (errors[key] ?? errors[key[0].toUpperCase() + key.slice(1)])?.join(" ");
  const update = <K extends Exclude<keyof EntryDraft, "expectedVersion">>(key: K, value: EntryDraft[K]) => setDraft((current) => ({ ...current, [key]: value }));
  const onError = (error: unknown) => setFailure(error);
  const reloadAfterChange = { onSuccess: () => onReload?.(), onError };
  const locales = siteLocales(site);
  const run = (action: () => void) => { setFailure(undefined); action(); };
  const saveCurrent = () => run(() => {
    const input = { slug: draft.slug, canonicalPath: draft.canonicalPath, title: draft.title, excerpt: draft.excerpt, fields: draft.fields as ContentEntryFields };
    if (entry && expectedVersion !== null) save.mutate({ id: entry.id, input: { ...input, expectedVersion } }, reloadAfterChange);
    else if (!entry) create.mutate({ ...input, siteId: site.id, contentTypeId: type.id }, { onSuccess: (outcome) => onSaved(outcome.id), onError });
  });

  return <div className="flex flex-col gap-6">
    <div className="flex flex-col gap-4">
      <div><StudioButton label={t("contentsStudio.backToEntries")} icon="fa-arrow-left rtl:-scale-x-100" variant="ghost" onPress={onBack} /></div>
      <div className="flex flex-wrap items-center justify-between gap-4">
        <div className="flex min-w-0 flex-wrap items-center gap-3">
          <h2 className="text-xl font-semibold"><bdi dir="auto">{draft.title || (entry ? entry.title : t("contentsStudio.newEntry"))}</bdi></h2>
          {entry && <EntryStatusChip status={entry.status} />}
          <span className="text-base text-muted">{type.name}</span>
        </div>
        <div className="flex flex-wrap items-end gap-3">
          {locales.length > 1 && <StudioSelect className="min-w-40" label={t("contentsStudio.language")} value={locale} onChange={setLocale} options={locales.map((id) => ({ id, label: id }))} />}
          {entry && <StudioButton label={t("contentsStudio.revisions")} icon="fa-clock-rotate-left" variant="tertiary" onPress={() => setHistoryOpen(true)} />}
        </div>
      </div>
    </div>

    {isConflict(failure) && <Alert status="warning">
      <Alert.Indicator />
      <Alert.Content><Alert.Title className="text-base">{t("contentsStudio.conflict")}</Alert.Title></Alert.Content>
      {onReload && <StudioButton label={t("contentsStudio.reload")} icon="fa-rotate" onPress={onReload} />}
    </Alert>}
    {Boolean(failure) && !isConflict(failure) && <StudioFailure error={failure} />}

    <Surface className="flex flex-col gap-6 rounded-3xl p-6" render={renderSection} aria-labelledby="contents-entry-details">
      <div className="flex items-center gap-3"><HeaderIcon icon="circle-info" /><h2 id="contents-entry-details" className="text-lg font-semibold text-foreground">{t("contentsStudio.details")}</h2></div>
      <StudioInput label={t("contentsStudio.entryTitle")} value={draft.title} onChange={(v) => update("title", v)} error={fieldError("title")} />
      <div className="grid gap-5 md:grid-cols-2">
        <StudioInput label={t("contentsStudio.slug")} value={draft.slug} onChange={(v) => update("slug", v)} error={fieldError("slug")} />
        <StudioInput label={t("contentsStudio.path")} value={draft.canonicalPath} onChange={(v) => update("canonicalPath", v)} error={fieldError("canonicalPath")} />
      </div>
      <StudioTextArea label={t("contentsStudio.excerpt")} value={draft.excerpt} onChange={(v) => update("excerpt", v)} error={fieldError("excerpt")} />
    </Surface>

    {type.fields.length > 0 && <Surface className="flex flex-col gap-6 rounded-3xl p-6" render={renderSection} aria-labelledby="contents-entry-fields">
      <div className="flex items-center gap-3"><HeaderIcon icon="table-list" /><h2 id="contents-entry-fields" className="text-lg font-semibold text-foreground">{t("contentsStudio.fields")}</h2></div>
      {type.fields.map((field) => <ContentFieldEditor key={field.key} definition={field} value={draft.fields[field.key]} onChange={(next) => update("fields", updateFieldValue(draft.fields, field.key, next))} locale={locale} siteId={site.id} path={`fields.${field.key}`} errors={errors} />)}
      {fieldError("fields") && <p className="text-base text-danger" role="alert">{fieldError("fields")}</p>}
    </Surface>}

    <Surface className="sticky bottom-4 z-10 flex flex-wrap items-center gap-3 rounded-3xl p-4 shadow-overlay">
      <StudioButton label={t("contentsStudio.saveDraft")} icon="fa-floppy-disk" variant="primary" disabled={pending} onPress={saveCurrent} />
      {entry && <>
        <StudioButton label={t("contentsStudio.publish")} icon="fa-cloud-arrow-up" disabled={pending || expectedVersion === null || !entry.workingRevisionId} onPress={() => run(() => { if (expectedVersion !== null && entry.workingRevisionId) publish.mutate({ id: entry.id, revisionId: entry.workingRevisionId, expectedVersion }, reloadAfterChange); })} />
        <span className="flex-1" />
        <StudioButton label={t("contentsStudio.unpublish")} icon="fa-eye-slash" variant="ghost" disabled={pending || expectedVersion === null} onPress={() => run(() => { if (expectedVersion !== null) unpublish.mutate({ id: entry.id, expectedVersion }, reloadAfterChange); })} />
        <StudioButton label={t("contentsStudio.archive")} icon="fa-box-archive" variant="ghost" disabled={pending || expectedVersion === null} onPress={() => run(() => { if (expectedVersion !== null) archive.mutate({ id: entry.id, expectedVersion }, { onSuccess: onBack, onError }); })} />
      </>}
    </Surface>

    {entry && <RevisionHistory entry={entry} expectedVersion={expectedVersion} pending={pending} isOpen={historyOpen} onOpenChange={setHistoryOpen} onRestore={(revisionId) => run(() => { if (expectedVersion !== null) restore.mutate({ id: entry.id, revisionId, expectedVersion }, { onSuccess: () => { setHistoryOpen(false); onReload?.(); }, onError }); })} />}
  </div>;
}

/** Edits a stored entry; a reload re-reads it and starts the form from the fresh version. */
export function EntryEditor({ id, sites, types, onBack }: { id: string; sites: ContentSite[]; types: ContentType[]; onBack: () => void }) {
  const { t } = useLocalizedText();
  const [generation, setGeneration] = useState(0);
  const entry = useContentEntry(id);
  if (entry.isLoading) return <StudioState>{t("contentsStudio.loading")}</StudioState>;
  if (entry.isError || !entry.data) return <StudioFailure error={entry.error} />;
  const site = sites.find((item) => item.id === entry.data.siteId);
  const type = types.find((item) => item.id === entry.data.contentTypeId);
  if (!site || !type) return <StudioState error>{t("contentsStudio.error")}</StudioState>;
  const reload = () => { void entry.refetch().then((result) => { if (result.isSuccess) setGeneration((n) => n + 1); }); };
  return <EntryForm key={generation} entry={entry.data} site={site} type={type} onBack={onBack} onSaved={() => undefined} onReload={reload} />;
}

/** Creates an entry of a chosen type, then continues in the stored entry's editor. */
export function NewEntryEditor({ site, types, initialTypeId, onBack, onCreated }: { site: ContentSite; types: ContentType[]; initialTypeId: string; onBack: () => void; onCreated: (id: string) => void }) {
  const { t } = useLocalizedText();
  const [typeId, setTypeId] = useState(initialTypeId || (types.length === 1 ? types[0].id : ""));
  const type = types.find((item) => item.id === typeId);
  if (!type) return <div className="flex flex-col gap-6">
    <div><StudioButton label={t("contentsStudio.backToEntries")} icon="fa-arrow-left rtl:-scale-x-100" variant="ghost" onPress={onBack} /></div>
    <Surface className="flex max-w-xl flex-col gap-4 rounded-3xl p-6">
      <h2 className="text-xl font-semibold">{t("contentsStudio.newEntry")}</h2>
      <StudioSelect label={t("contentsStudio.selectType")} value={typeId} onChange={setTypeId} options={types.map((item) => ({ id: item.id, label: item.name }))} />
    </Surface>
  </div>;
  return <EntryForm key={type.id} site={site} type={type} onBack={onBack} onSaved={onCreated} />;
}

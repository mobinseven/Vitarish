"use client";

import { Surface } from "@heroui/react";
import { formatDateTime, useLocalizedText } from "@vitarish/localization";
import { HeaderIcon } from "@vitarish/ui";
import { contentEntryStatuses, type ContentSite, type ContentType } from "./contentsStudioApi";
import { EntryStatusChip, renderSection, StudioButton, StudioFailure, StudioSelect, StudioState } from "./StudioControls";
import { useContentEntries } from "./useContentsStudio";

export const entryListPageSize = 20;

export interface EntryListFilter {
  siteId: string;
  typeId: string;
  status: string;
  page: number;
}

/** The studio's landing page: filter, then open an entry by its row. */
export function EntryList({ sites, types, filter, onFilterChange, onOpen, onNew }: {
  sites: ContentSite[];
  types: ContentType[];
  filter: EntryListFilter;
  onFilterChange: (filter: EntryListFilter) => void;
  onOpen: (id: string) => void;
  onNew: () => void;
}) {
  const { t, locale } = useLocalizedText();
  const entries = useContentEntries({ siteId: filter.siteId, typeId: filter.typeId || undefined, status: filter.status || undefined, skip: filter.page * entryListPageSize, take: entryListPageSize });
  const typeName = (id: string) => types.find((type) => type.id === id)?.name ?? "";
  const change = (next: Partial<EntryListFilter>) => onFilterChange({ ...filter, page: 0, ...next });
  const rows = entries.data ?? [];

  return <Surface className="flex flex-col gap-6 rounded-3xl p-6" render={renderSection} aria-labelledby="contents-entries-title">
    <div className="flex flex-col items-start gap-5 sm:flex-row sm:items-center sm:justify-between">
      <div className="flex items-center gap-3">
        <HeaderIcon icon="file-lines" />
        <h2 id="contents-entries-title" className="text-lg font-semibold text-foreground">{t("contentsStudio.entries")}</h2>
      </div>
      <StudioButton label={t("contentsStudio.newEntry")} icon="fa-plus" variant="primary" disabled={!filter.siteId || types.length === 0} onPress={onNew} />
    </div>

    <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-3">
      {sites.length > 1 && <StudioSelect label={t("contentsStudio.site")} value={filter.siteId} onChange={(siteId) => change({ siteId })} options={sites.map((site) => ({ id: site.id, label: site.name }))} />}
      <StudioSelect label={t("contentsStudio.type")} value={filter.typeId} onChange={(typeId) => change({ typeId })} options={[{ id: "", label: t("contentsStudio.all") }, ...types.map((type) => ({ id: type.id, label: type.name }))]} />
      <StudioSelect label={t("contentsStudio.status")} value={filter.status} onChange={(status) => change({ status })} options={["", ...contentEntryStatuses].map((id) => ({ id, label: t(`contentsStudio.${id || "all"}`) }))} />
    </div>

    {entries.isLoading && <StudioState>{t("contentsStudio.loading")}</StudioState>}
    {entries.isError && <StudioFailure error={entries.error} />}
    {entries.isSuccess && rows.length === 0 && <StudioState>{t("contentsStudio.empty")}</StudioState>}
    {rows.length > 0 && <ul className="flex flex-col gap-3" aria-labelledby="contents-entries-title">
      {rows.map((entry) => <li key={entry.id}>
        <Surface variant="secondary" className="relative flex flex-wrap items-center gap-x-4 gap-y-2 rounded-2xl p-4 transition-colors hover:bg-default">
          <span className="flex min-w-0 flex-1 basis-full flex-col gap-1 sm:basis-0">
            <bdi dir="auto" className="max-w-full self-start truncate text-base font-semibold">{entry.title}</bdi>
            <span className="text-base text-muted"><bdi dir="ltr">{entry.canonicalPath}</bdi> · {typeName(entry.contentTypeId)}</span>
          </span>
          {entry.publishedAt && <span className="text-base text-muted">{t("contentsStudio.publishedOn", { date: formatDateTime(entry.publishedAt, locale) })}</span>}
          <EntryStatusChip status={entry.status} />
          {/* The edit action covers the whole row, so the row opens the entry. */}
          <StudioButton label={t("contentsStudio.edit")} icon="fa-pen" variant="ghost" className="after:absolute after:inset-0 after:rounded-2xl after:content-['']" onPress={() => onOpen(entry.id)} />
        </Surface>
      </li>)}
    </ul>}

    {(filter.page > 0 || rows.length === entryListPageSize) && <nav className="flex items-center justify-center gap-3" aria-label={t("contentsStudio.pagination")}>
      <StudioButton label={t("contentsStudio.previous")} icon="fa-chevron-left rtl:-scale-x-100" disabled={filter.page === 0} onPress={() => onFilterChange({ ...filter, page: filter.page - 1 })} />
      <span className="text-base">{t("contentsStudio.page", { page: filter.page + 1 })}</span>
      <StudioButton label={t("contentsStudio.next")} icon="fa-chevron-right rtl:-scale-x-100" disabled={rows.length < entryListPageSize} onPress={() => onFilterChange({ ...filter, page: filter.page + 1 })} />
    </nav>}
  </Surface>;
}

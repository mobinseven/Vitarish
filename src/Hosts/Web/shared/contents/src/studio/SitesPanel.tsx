"use client";

import { Surface } from "@heroui/react";
import { ApiError } from "@vitarish/api-client";
import { useLocalizedText } from "@vitarish/localization";
import { HeaderIcon } from "@vitarish/ui";
import { useState } from "react";
import type { ContentSite, ContentSiteRequest } from "./contentsStudioApi";
import { StudioButton, StudioFailure, StudioInput, StudioState } from "./StudioControls";
import { useContentSites, useCreateContentSite, useUpdateContentSite } from "./useContentsStudio";

interface SiteDraft {
  key: string;
  name: string;
  defaultLocale: string;
  locales: string;
}

const siteProblemFieldNames = {
  key: "Key",
  name: "Name",
  defaultLocale: "DefaultLocale",
  locales: "Locales",
} as const;

function emptySiteDraft(locale: string): SiteDraft {
  return { key: "", name: "", defaultLocale: locale, locales: locale };
}

function siteDraftFromSite(site: ContentSite): SiteDraft {
  return {
    key: site.key,
    name: site.name,
    defaultLocale: site.defaultLocale,
    locales: site.locales.join(", "),
  };
}

function hasFieldProblem(error: unknown, field: keyof typeof siteProblemFieldNames): boolean {
  if (!(error instanceof ApiError)) return false;
  return Boolean(error.problem.errors?.[siteProblemFieldNames[field]]?.length);
}

function requestFromDraft(draft: SiteDraft): ContentSiteRequest {
  return {
    key: draft.key,
    name: draft.name,
    defaultLocale: draft.defaultLocale,
    locales: draft.locales.split(",").map((item) => item.trim()).filter((item) => item.length > 0),
  };
}

export function SitesPanel() {
  const { t, locale } = useLocalizedText();
  const sites = useContentSites();
  const createSite = useCreateContentSite();
  const updateSite = useUpdateContentSite();
  const [formOpen, setFormOpen] = useState(false);
  const [editingSiteId, setEditingSiteId] = useState<string | null>(null);
  const [draft, setDraft] = useState<SiteDraft>(() => emptySiteDraft(locale));
  const failure = updateSite.error ?? createSite.error;
  const pending = updateSite.isPending || createSite.isPending;

  const clearEditor = () => {
    setFormOpen(false);
    setEditingSiteId(null);
    setDraft(emptySiteDraft(locale));
    createSite.reset();
    updateSite.reset();
  };

  const openNewSite = () => {
    clearEditor();
    setFormOpen(true);
  };

  const openEditSite = (site: ContentSite) => {
    createSite.reset();
    updateSite.reset();
    setDraft(siteDraftFromSite(site));
    setEditingSiteId(site.id);
    setFormOpen(true);
  };

  const saveSite = () => {
    createSite.reset();
    updateSite.reset();
    const input = requestFromDraft(draft);
    const callbacks = { onSuccess: clearEditor };
    if (editingSiteId) updateSite.mutate({ id: editingSiteId, input }, callbacks);
    else createSite.mutate(input, callbacks);
  };

  return (
    <Surface className="flex flex-col gap-6 rounded-3xl p-6" role="region" aria-labelledby="contents-sites-title">
      <div className="flex flex-col items-start gap-5 sm:flex-row sm:items-center sm:justify-between">
        <h2 id="contents-sites-title" className="flex items-center gap-3 text-lg font-semibold text-foreground"><HeaderIcon icon="globe" />
          {t("contentsStudio.sites")}
        </h2>
        <StudioButton label={t("contentsStudio.newSite")} icon="fa-plus" variant="primary" disabled={pending} onPress={openNewSite} />
      </div>

      {sites.isLoading && <StudioState>{t("contentsStudio.loading")}</StudioState>}
      {sites.isError && <StudioFailure error={sites.error} />}
      {sites.isSuccess && sites.data.length === 0 && <StudioState>{t("contentsStudio.empty")}</StudioState>}
      {sites.isSuccess && sites.data.length > 0 && (
        <div className="flex flex-col gap-3" aria-label={t("contentsStudio.sites")}>
          {sites.data.map((site) => (
            <Surface key={site.id} variant="secondary" className="flex flex-col gap-4 rounded-2xl p-4 sm:flex-row sm:items-center sm:justify-between">
              <div className="flex min-w-0 flex-col gap-2">
                <h3 className="text-base font-semibold">{site.name}</h3>
                <p className="text-base"><span>{t("contentsStudio.key")}:</span> <bdi dir="auto">{site.key}</bdi></p>
                <p className="text-base"><span>{t("contentsStudio.defaultLocale")}:</span> <bdi dir="auto">{site.defaultLocale}</bdi></p>
                <p className="text-base"><span>{t("contentsStudio.locales")}:</span> <bdi dir="auto">{site.locales.join(", ")}</bdi></p>
              </div>
              <StudioButton
                label={`${t("contentsStudio.edit")} ${site.name}`}
                icon="fa-pen"
                disabled={pending}
                onPress={() => openEditSite(site)}
              />
            </Surface>
          ))}
        </div>
      )}

      {formOpen && (
        <div className="flex flex-col gap-4">
          <h3 className="text-lg font-semibold text-foreground">
            {editingSiteId ? t("contentsStudio.edit") : t("contentsStudio.newSite")}
          </h3>
          <div className="grid gap-4 md:grid-cols-2">
            <StudioInput label={t("contentsStudio.key")} value={draft.key} onChange={(key) => setDraft((current) => ({ ...current, key }))} error={hasFieldProblem(failure, "key") ? t("contentsStudio.validation") : undefined} />
            <StudioInput label={t("contentsStudio.name")} value={draft.name} onChange={(name) => setDraft((current) => ({ ...current, name }))} error={hasFieldProblem(failure, "name") ? t("contentsStudio.validation") : undefined} />
            <StudioInput label={t("contentsStudio.defaultLocale")} value={draft.defaultLocale} onChange={(defaultLocale) => setDraft((current) => ({ ...current, defaultLocale }))} error={hasFieldProblem(failure, "defaultLocale") ? t("contentsStudio.validation") : undefined} />
            <StudioInput label={t("contentsStudio.locales")} value={draft.locales} onChange={(locales) => setDraft((current) => ({ ...current, locales }))} error={hasFieldProblem(failure, "locales") ? t("contentsStudio.validation") : undefined} />
          </div>
          {failure && <StudioFailure error={failure} />}
          <div className="flex flex-wrap gap-3">
            <StudioButton label={t("contentsStudio.save")} icon="fa-floppy-disk" variant="primary" disabled={pending} onPress={saveSite} />
            <StudioButton label={t("contentsStudio.cancel")} icon="fa-xmark" disabled={pending} onPress={clearEditor} />
          </div>
        </div>
      )}
    </Surface>
  );
}

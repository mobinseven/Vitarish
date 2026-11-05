"use client";

import { Tabs } from "@heroui/react";
import { useLocalizedText } from "@vitarish/localization";
import { EntriesPanel } from "./EntriesPanel";
import { SitesPanel } from "./SitesPanel";
import { StudioFailure, StudioState } from "./StudioControls";
import { TypesPanel } from "./TypesPanel";
import { useContentSites, useContentTypes } from "./useContentsStudio";

export function ContentStudio() {
  const { t } = useLocalizedText();
  const sites = useContentSites();
  const types = useContentTypes();
  return <div className="flex flex-col gap-6"><h1 className="text-2xl font-semibold">{t("contentsStudio.title")}</h1>
    <Tabs><Tabs.ListContainer><Tabs.List aria-label={t("contentsStudio.title")}>
      <Tabs.Tab id="sites">{t("contentsStudio.sites")}<Tabs.Indicator /></Tabs.Tab>
      <Tabs.Tab id="types">{t("contentsStudio.types")}<Tabs.Indicator /></Tabs.Tab>
      <Tabs.Tab id="entries">{t("contentsStudio.entries")}<Tabs.Indicator /></Tabs.Tab>
    </Tabs.List></Tabs.ListContainer>
      <Tabs.Panel id="sites"><SitesPanel /></Tabs.Panel>
      <Tabs.Panel id="types"><TypesPanel /></Tabs.Panel>
      <Tabs.Panel id="entries">{sites.isLoading || types.isLoading ? <StudioState>{t("contentsStudio.loading")}</StudioState> : sites.isError || types.isError ? <StudioFailure error={sites.error ?? types.error} /> : <EntriesPanel sites={sites.data ?? []} types={types.data ?? []} />}</Tabs.Panel>
    </Tabs>
  </div>;
}

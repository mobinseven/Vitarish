"use client";

import { Tabs } from "@heroui/react";
import { useLocalizedText } from "@vitarish/localization";
import { PageHeader } from "@vitarish/ui";
import { useEffect, useState, type ReactNode } from "react";
import styles from "./content-studio.module.css";
import { EntryEditor, NewEntryEditor } from "./EntryEditor";
import { EntryList, type EntryListFilter } from "./EntryList";
import { SitesPanel } from "./SitesPanel";
import { StudioFailure, StudioState } from "./StudioControls";
import { TypesPanel } from "./TypesPanel";
import { useContentSites, useContentTypes } from "./useContentsStudio";

type StudioArea = "content" | "structure";
type ContentView = { page: "list" } | { page: "entry"; id: string } | { page: "new" };

/**
 * The content studio. Editors land on the entry list; sites and types live under Structure.
 * The host owns the page shell and supplies header actions, such as the way back and signing out.
 */
export function ContentStudio({ headerActions }: { headerActions?: ReactNode }) {
  const { t } = useLocalizedText();
  const sites = useContentSites();
  const types = useContentTypes();
  const [area, setArea] = useState<StudioArea>("content");
  const [view, setView] = useState<ContentView>({ page: "list" });
  const [filter, setFilter] = useState<EntryListFilter>({ siteId: "", typeId: "", status: "", page: 0 });
  const firstSiteId = sites.data?.[0]?.id ?? "";
  useEffect(() => {
    if (!filter.siteId && firstSiteId) setFilter((current) => ({ ...current, siteId: firstSiteId }));
  }, [filter.siteId, firstSiteId]);
  const site = sites.data?.find((item) => item.id === filter.siteId);
  const toList = () => setView({ page: "list" });

  let content: ReactNode;
  if (sites.isLoading || types.isLoading) content = <StudioState>{t("contentsStudio.loading")}</StudioState>;
  else if (sites.isError || types.isError) content = <StudioFailure error={sites.error ?? types.error} />;
  else if (view.page === "entry") content = <EntryEditor key={view.id} id={view.id} sites={sites.data ?? []} types={types.data ?? []} onBack={toList} />;
  else if (view.page === "new" && site) content = <NewEntryEditor site={site} types={types.data ?? []} initialTypeId={filter.typeId} onBack={toList} onCreated={(id) => setView({ page: "entry", id })} />;
  else content = <EntryList sites={sites.data ?? []} types={types.data ?? []} filter={filter} onFilterChange={setFilter} onOpen={(id) => setView({ page: "entry", id })} onNew={() => setView({ page: "new" })} />;

  return <div className={`${styles.studio} flex flex-col gap-6`}>
    <PageHeader
      icon="pen-nib"
      title={t("contentsStudio.title")}
      actions={headerActions}
      tabs={<Tabs selectedKey={area} onSelectionChange={(key) => { setArea(key as StudioArea); toList(); }}>
        <Tabs.ListContainer><Tabs.List aria-label={t("contentsStudio.title")}>
          <Tabs.Tab id="content">{t("contentsStudio.content")}<Tabs.Indicator /></Tabs.Tab>
          <Tabs.Tab id="structure">{t("contentsStudio.structure")}<Tabs.Indicator /></Tabs.Tab>
        </Tabs.List></Tabs.ListContainer>
      </Tabs>}
    />
    {area === "content" ? content : <div className="flex flex-col gap-6"><SitesPanel /><TypesPanel /></div>}
  </div>;
}

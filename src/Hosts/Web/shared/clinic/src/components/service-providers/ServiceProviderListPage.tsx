"use client";
import { useState, type ReactNode } from "react";
import { ErrorStateSurface as ClinicErrorState, LoadingStateSurface as ClinicLoadingState, PageHeader } from "@vitarish/ui";
import { pageAncestors, usePageBack } from "../navigation/usePageBack";
import { useLocalizedText } from "../../localization/index";
import type { NavigationIntent } from "../../navigation";
import { ServiceProviderDeleteDialog } from "./ServiceProviderDeleteDialog";
import { ServiceProviderList } from "./ServiceProviderList";
import {
  useDeleteServiceProvider,
  useServiceProviders,
} from "../../hooks/service-providers/useServiceProviders";

export function ServiceProviderListPage({ onNavigate, tabs }: { onNavigate?: (intent: NavigationIntent) => void; tabs?: ReactNode }) {
  const { t } = useLocalizedText();
  const back = usePageBack([pageAncestors.settings], onNavigate);
  const query = useServiceProviders();
  const deletion = useDeleteServiceProvider();
  const [deleteId, setDeleteId] = useState<string | null>(null);
  const confirm = () => {
    if (deleteId)
      deletion.mutate(deleteId, { onSuccess: () => setDeleteId(null) });
  };
  return (
    <section
      className="flex flex-col gap-6"
      aria-labelledby="clinic-service-providers-title"
    >
      <PageHeader
        icon="user-doctor"
        title={t("clinic.serviceProviders.title")}
        titleId="clinic-service-providers-title"
        details={t("clinic.serviceProviders.description")}
        back={back}
        tabs={tabs}
      />
      {query.isLoading ? (
        <ClinicLoadingState label={t("common.states.loading")} />
      ) : null}
      {query.error ? (
        <ClinicErrorState title={t("clinic.serviceProviders.failedToLoad")} />
      ) : null}
      {!query.isLoading && !query.error ? (
        <ServiceProviderList items={query.data ?? []} onDelete={setDeleteId} />
      ) : null}
      {deletion.isError ? (
        <ClinicErrorState title={t("clinic.serviceProviders.deleteFailed")} />
      ) : null}
      <ServiceProviderDeleteDialog
        isOpen={deleteId !== null}
        isPending={deletion.isPending}
        onCancel={() => setDeleteId(null)}
        onConfirm={confirm}
      />
    </section>
  );
}

"use client";
import { useState } from "react";
import {
  ErrorStateSurface as ClinicErrorState,
  LoadingStateSurface as ClinicLoadingState,
} from "@vitarish/ui";
import { useLocalizedText } from "../../localization/index";
import { ServiceProviderDeleteDialog } from "./ServiceProviderDeleteDialog";
import { ServiceProviderList } from "./ServiceProviderList";
import {
  useDeleteServiceProvider,
  useServiceProviders,
} from "../../hooks/service-providers/useServiceProviders";

export function ServiceProviderListPage() {
  const { t } = useLocalizedText();
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
      <header>
        <h1 id="clinic-service-providers-title" className="text-2xl font-bold">
          {t("clinic.serviceProviders.title")}
        </h1>
        <p className="text-sm text-muted">
          {t("clinic.serviceProviders.description")}
        </p>
      </header>
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

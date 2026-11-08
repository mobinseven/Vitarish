"use client";
import { useState, type ReactNode } from "react";
import { ErrorStateSurface as ClinicErrorState, LoadingStateSurface as ClinicLoadingState, PageHeader } from "@vitarish/ui";
import { pageAncestors, usePageBack } from "../navigation/usePageBack";
import { useLocalizedText } from "../../localization/index";
import type { NavigationIntent } from "../../navigation";
import { ServiceTypeDeleteDialog } from "./ServiceTypeDeleteDialog";
import { ServiceTypeList } from "./ServiceTypeList";
import {
  useDeleteServiceType,
  useServiceTypes,
} from "../../hooks/service-types/useServiceTypes";

export function ServiceTypeListPage({ onNavigate, tabs }: { onNavigate?: (intent: NavigationIntent) => void; tabs?: ReactNode }) {
  const { t } = useLocalizedText();
  const back = usePageBack([pageAncestors.settings], onNavigate);
  const query = useServiceTypes();
  const deletion = useDeleteServiceType();
  const [deleteId, setDeleteId] = useState<string | null>(null);
  const confirm = () => {
    if (deleteId)
      deletion.mutate(deleteId, { onSuccess: () => setDeleteId(null) });
  };
  return (
    <section
      className="flex flex-col gap-6"
      aria-labelledby="clinic-service-types-title"
    >
      <PageHeader
        icon="tags"
        title={t("clinic.serviceTypes.title")}
        titleId="clinic-service-types-title"
        details={t("clinic.serviceTypes.description")}
        back={back}
        tabs={tabs}
      />
      {query.isLoading ? (
        <ClinicLoadingState label={t("common.states.loading")} />
      ) : null}
      {query.error ? (
        <ClinicErrorState title={t("clinic.serviceTypes.failedToLoad")} />
      ) : null}
      {!query.isLoading && !query.error ? (
        <ServiceTypeList items={query.data ?? []} onDelete={setDeleteId} />
      ) : null}
      {deletion.isError ? (
        <ClinicErrorState title={t("clinic.serviceTypes.deleteFailed")} />
      ) : null}
      <ServiceTypeDeleteDialog
        isOpen={deleteId !== null}
        isPending={deletion.isPending}
        onCancel={() => setDeleteId(null)}
        onConfirm={confirm}
      />
    </section>
  );
}

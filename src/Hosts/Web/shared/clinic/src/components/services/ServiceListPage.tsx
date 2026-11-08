"use client";

import { useState, type ReactNode } from "react";
import { ErrorStateSurface as ClinicErrorState, LoadingStateSurface as ClinicLoadingState, PageHeader } from "@vitarish/ui";
import { pageAncestors, usePageBack } from "../navigation/usePageBack";
import { useLocalizedText } from "../../localization/index";
import type { NavigationIntent } from "../../navigation";
import { ServiceDeleteDialog } from "./ServiceDeleteDialog";
import { ServiceList } from "./ServiceList";
import {
  useDeleteService,
  useServices,
} from "../../hooks/services/useServices";

export function ServiceListPage({ onNavigate, tabs }: { onNavigate?: (intent: NavigationIntent) => void; tabs?: ReactNode }) {
  const { t } = useLocalizedText();
  const back = usePageBack([pageAncestors.settings], onNavigate);
  const services = useServices();
  const deleteService = useDeleteService();
  const [deleteId, setDeleteId] = useState<string | null>(null);

  const confirmDelete = () => {
    if (!deleteId) return;
    deleteService.mutate(deleteId, { onSuccess: () => setDeleteId(null) });
  };

  return (
    <section
      className="flex flex-col gap-6"
      aria-labelledby="clinic-services-title"
    >
      <PageHeader
        icon="briefcase-medical"
        title={t("clinic.services.title")}
        titleId="clinic-services-title"
        details={t("clinic.services.description")}
        back={back}
        tabs={tabs}
      />
      {services.isLoading ? (
        <ClinicLoadingState label={t("common.states.loading")} />
      ) : null}
      {services.error ? (
        <ClinicErrorState title={t("clinic.services.failedToLoad")} />
      ) : null}
      {!services.isLoading && !services.error ? (
        <ServiceList items={services.data ?? []} onDelete={setDeleteId} />
      ) : null}
      {deleteService.isError ? (
        <ClinicErrorState title={t("clinic.services.deleteFailed")} />
      ) : null}
      <ServiceDeleteDialog
        isOpen={deleteId !== null}
        isPending={deleteService.isPending}
        onCancel={() => setDeleteId(null)}
        onConfirm={confirmDelete}
      />
    </section>
  );
}

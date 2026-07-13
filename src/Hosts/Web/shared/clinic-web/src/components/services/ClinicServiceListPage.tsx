"use client";

import { useState } from "react";
import { ErrorStateSurface as ClinicErrorState, LoadingStateSurface as ClinicLoadingState } from "@vitarish/web-ui";
import { useLocalizedText } from "../../localization/index";
import { ServiceDeleteDialog } from "./ServiceDeleteDialog";
import { ServiceList } from "./ServiceList";
import { useDeleteService, useServices } from "../../hooks/services/useServices";

export function ClinicServiceListPage() {
  const { t } = useLocalizedText();
  const services = useServices();
  const deleteService = useDeleteService();
  const [deleteId, setDeleteId] = useState<string | null>(null);

  const confirmDelete = () => {
    if (!deleteId) return;
    deleteService.mutate(deleteId, { onSuccess: () => setDeleteId(null) });
  };

  return (
    <section className="flex flex-col gap-6" aria-labelledby="clinic-services-title">
      <header>
        <h1 id="clinic-services-title" className="text-2xl font-bold">{t("clinic.services.title")}</h1>
        <p className="text-sm text-muted">{t("clinic.services.description")}</p>
      </header>
      {services.isLoading ? <ClinicLoadingState label={t("common.states.loading")} /> : null}
      {services.error ? <ClinicErrorState title={t("clinic.services.failedToLoad")} /> : null}
      {!services.isLoading && !services.error ? (
        <ServiceList items={services.data ?? []} onDelete={setDeleteId} />
      ) : null}
      {deleteService.isError ? <ClinicErrorState title={t("clinic.services.deleteFailed")} /> : null}
      <ServiceDeleteDialog
        isOpen={deleteId !== null}
        isPending={deleteService.isPending}
        onCancel={() => setDeleteId(null)}
        onConfirm={confirmDelete}
      />
    </section>
  );
}

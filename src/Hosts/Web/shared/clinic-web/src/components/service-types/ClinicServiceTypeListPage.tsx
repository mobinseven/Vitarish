"use client";
import { useState } from "react";
import { ErrorStateSurface as ClinicErrorState, LoadingStateSurface as ClinicLoadingState } from "@vitarish/web-ui";
import { useLocalizedText } from "../../localization/index";
import { ServiceTypeDeleteDialog } from "./ServiceTypeDeleteDialog";
import { ServiceTypeList } from "./ServiceTypeList";
import { useDeleteServiceType, useServiceTypes } from "../../hooks/service-types/useServiceTypes";

export function ClinicServiceTypeListPage() {
  const { t } = useLocalizedText();
  const query = useServiceTypes();
  const deletion = useDeleteServiceType();
  const [deleteId, setDeleteId] = useState<string | null>(null);
  const confirm = () => { if (deleteId) deletion.mutate(deleteId, { onSuccess: () => setDeleteId(null) }); };
  return <section className="flex flex-col gap-6" aria-labelledby="clinic-service-types-title">
    <header><h1 id="clinic-service-types-title" className="text-2xl font-bold">{t("clinic.serviceTypes.title")}</h1><p className="text-sm text-muted">{t("clinic.serviceTypes.description")}</p></header>
    {query.isLoading ? <ClinicLoadingState label={t("common.states.loading")} /> : null}
    {query.error ? <ClinicErrorState title={t("clinic.serviceTypes.failedToLoad")} /> : null}
    {!query.isLoading && !query.error ? <ServiceTypeList items={query.data ?? []} onDelete={setDeleteId} /> : null}
    {deletion.isError ? <ClinicErrorState title={t("clinic.serviceTypes.deleteFailed")} /> : null}
    <ServiceTypeDeleteDialog isOpen={deleteId !== null} isPending={deletion.isPending} onCancel={() => setDeleteId(null)} onConfirm={confirm} />
  </section>;
}

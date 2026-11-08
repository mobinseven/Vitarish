"use client";
import { useState } from "react";
import { Button } from "@heroui/react";
import { DetailGrid, PageHeader, type DetailGridItem } from "@vitarish/ui";
import { pageAncestors, usePageBack } from "../navigation/usePageBack";
import type { NavigationIntent } from "../../navigation";
import { formatDateTime, useLocalizedText } from "../../localization";
import {
  ErrorStateSurface as ClinicErrorState,
  LoadingStateSurface as ClinicLoadingState,
} from "@vitarish/ui";
import { ServiceDeleteDialog } from "./ServiceDeleteDialog";
import { ServiceEditor } from "./ServiceEditor";
import {
  useDeleteService,
  useServiceDetail,
  useUpdateService,
} from "../../hooks/services/useServices";
export interface ServiceDetailPageProps {
  serviceId: string;
  onNavigate: (intent: NavigationIntent) => void;
}
export function ServiceDetailPage({
  serviceId,
  onNavigate,
}: ServiceDetailPageProps) {
  const { t, locale } = useLocalizedText();
  const back = usePageBack([pageAncestors.settings, pageAncestors.services], onNavigate);
  const query = useServiceDetail(serviceId);
  const update = useUpdateService();
  const deletion = useDeleteService();
  const [editing, setEditing] = useState(false);
  const [deleting, setDeleting] = useState(false);
  if (query.isLoading)
    return <ClinicLoadingState label={t("common.states.loading")} />;
  if (query.error || !query.data)
    return <ClinicErrorState title={t("clinic.services.notFound")} />;
  const service = query.data;
  const items: DetailGridItem[] = [
    { label: t("clinic.fields.name"), value: service.name },
    {
      label: t("clinic.fields.description"),
      value: service.description ?? t("common.values.notAvailable"),
    },
    {
      label: t("clinic.fields.serviceType"),
      value: service.serviceTypeName ?? t("common.values.notAvailable"),
    },
    {
      label: t("clinic.fields.created"),
      value: formatDateTime(service.createdAt, locale),
    },
    ...(service.updatedAt
      ? [
          {
            label: t("clinic.fields.updated"),
            value: formatDateTime(service.updatedAt, locale),
          },
        ]
      : []),
  ];
  return (
    <section className="flex flex-col gap-6">
      <PageHeader
        icon="briefcase-medical"
        title={editing ? t("clinic.services.editTitle") : service.name}
        back={back}
        actions={
          <>
            {!editing ? (
              <Button variant="ghost" onPress={() => setEditing(true)}>
                {t("common.actions.edit")}
              </Button>
            ) : null}
            <Button variant="danger" onPress={() => setDeleting(true)}>
              {t("common.actions.delete")}
            </Button>
          </>
        }
      />
      {editing ? (
        <ServiceEditor
          initialValue={{
            name: service.name,
            description: service.description ?? "",
          }}
          isPending={update.isPending}
          failure={update.isError ? t("clinic.services.saveFailed") : undefined}
          onCancel={() => setEditing(false)}
          onSubmit={(value) =>
            update.mutate(
              {
                id: serviceId,
                name: value.name,
                description: value.description || undefined,
                serviceTypeId: service.serviceTypeId,
              },
              { onSuccess: () => setEditing(false) },
            )
          }
        />
      ) : (
        <DetailGrid items={items} />
      )}
      <ServiceDeleteDialog
        isOpen={deleting}
        isPending={deletion.isPending}
        onCancel={() => setDeleting(false)}
        onConfirm={() =>
          deletion.mutate(serviceId, {
            onSuccess: () => onNavigate({ kind: "serviceIndex" }),
          })
        }
      />
    </section>
  );
}

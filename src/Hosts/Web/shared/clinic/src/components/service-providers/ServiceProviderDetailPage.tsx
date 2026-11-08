"use client";
import { useState } from "react";
import { Button } from "@heroui/react";
import { DetailGrid, PageHeader } from "@vitarish/ui";
import { pageAncestors, usePageBack } from "../navigation/usePageBack";
import type { NavigationIntent } from "../../navigation";
import { formatDateTime, useLocalizedText } from "../../localization";
import {
  ErrorStateSurface as ClinicErrorState,
  LoadingStateSurface as ClinicLoadingState,
} from "@vitarish/ui";
import { ServiceProviderDeleteDialog } from "./ServiceProviderDeleteDialog";
import { ServiceProviderEditor } from "./ServiceProviderEditor";
import {
  useDeleteServiceProvider,
  useServiceProviderDetail,
  useUpdateServiceProvider,
} from "../../hooks/service-providers/useServiceProviders";
export interface ServiceProviderDetailPageProps {
  serviceProviderId: string;
  onNavigate: (intent: NavigationIntent) => void;
}
export function ServiceProviderDetailPage({
  serviceProviderId,
  onNavigate,
}: ServiceProviderDetailPageProps) {
  const { t, locale } = useLocalizedText();
  const back = usePageBack([pageAncestors.settings, pageAncestors.serviceProviders], onNavigate);
  const query = useServiceProviderDetail(serviceProviderId);
  const update = useUpdateServiceProvider();
  const deletion = useDeleteServiceProvider();
  const [editing, setEditing] = useState(false);
  const [deleting, setDeleting] = useState(false);
  if (query.isLoading)
    return <ClinicLoadingState label={t("common.states.loading")} />;
  if (query.error || !query.data)
    return <ClinicErrorState title={t("clinic.serviceProviders.notFound")} />;
  const item = query.data;
  return (
    <section className="flex flex-col gap-6">
      <PageHeader
        icon="user-doctor"
        title={editing ? t("clinic.serviceProviders.editTitle") : item.name}
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
        <ServiceProviderEditor
          initialValue={{
            name: item.name,
            description: item.description ?? "",
          }}
          isPending={update.isPending}
          failure={
            update.isError ? t("clinic.serviceProviders.saveFailed") : undefined
          }
          onCancel={() => setEditing(false)}
          onSubmit={(value) =>
            update.mutate(
              {
                id: serviceProviderId,
                name: value.name,
                description: value.description || undefined,
              },
              { onSuccess: () => setEditing(false) },
            )
          }
        />
      ) : (
        <DetailGrid
          items={[
            { label: t("clinic.fields.name"), value: item.name },
            {
              label: t("clinic.fields.description"),
              value: item.description ?? t("common.values.notAvailable"),
            },
            {
              label: t("clinic.fields.linkedUsers"),
              value: item.linkedUserIds.length,
            },
            {
              label: t("clinic.fields.created"),
              value: formatDateTime(item.createdAt, locale),
            },
            ...(item.updatedAt
              ? [
                  {
                    label: t("clinic.fields.updated"),
                    value: formatDateTime(item.updatedAt, locale),
                  },
                ]
              : []),
          ]}
        />
      )}
      <ServiceProviderDeleteDialog
        isOpen={deleting}
        isPending={deletion.isPending}
        onCancel={() => setDeleting(false)}
        onConfirm={() =>
          deletion.mutate(serviceProviderId, {
            onSuccess: () => onNavigate({ kind: "serviceProviderIndex" }),
          })
        }
      />
    </section>
  );
}

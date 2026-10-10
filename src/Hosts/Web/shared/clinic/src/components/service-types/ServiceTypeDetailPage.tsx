"use client";
import { useState } from "react";
import { Button } from "@heroui/react";
import {
  DetailGrid,
  HeaderIcon,
  actionRoleClassName,
} from "@vitarish/ui";
import type { NavigationIntent } from "../../navigation";
import { formatDateTime, useLocalizedText } from "../../localization";
import {
  ErrorStateSurface as ClinicErrorState,
  LoadingStateSurface as ClinicLoadingState,
} from "@vitarish/ui";
import { ServiceTypeDeleteDialog } from "./ServiceTypeDeleteDialog";
import { ServiceTypeEditor } from "./ServiceTypeEditor";
import {
  useDeleteServiceType,
  useServiceTypeDetail,
  useUpdateServiceType,
} from "../../hooks/service-types/useServiceTypes";
export interface ServiceTypeDetailPageProps {
  serviceTypeId: string;
  onNavigate: (intent: NavigationIntent) => void;
}
export function ServiceTypeDetailPage({
  serviceTypeId,
  onNavigate,
}: ServiceTypeDetailPageProps) {
  const { t, locale } = useLocalizedText();
  const query = useServiceTypeDetail(serviceTypeId);
  const update = useUpdateServiceType();
  const deletion = useDeleteServiceType();
  const [editing, setEditing] = useState(false);
  const [deleting, setDeleting] = useState(false);
  if (query.isLoading)
    return <ClinicLoadingState label={t("common.states.loading")} />;
  if (query.error || !query.data)
    return <ClinicErrorState title={t("clinic.serviceTypes.notFound")} />;
  const item = query.data;
  return (
    <section className="flex flex-col gap-6">
      <header className="flex justify-between">
        <div>
          <Button
            variant="ghost"
            onPress={() => onNavigate({ kind: "serviceTypeIndex" })}
          >
            {t("common.actions.previous")}
          </Button>
          <div className="flex items-center gap-4">
            <HeaderIcon context="catalog" icon="tags" level="page" />
            <div className="min-w-0">
              <h1 className="text-2xl font-bold">
                {editing ? t("clinic.serviceTypes.editTitle") : item.name}
              </h1>
            </div>
          </div>
        </div>
        <div className="flex gap-2">
          {!editing ? (
            <Button className={actionRoleClassName("edit")} variant="ghost" onPress={() => setEditing(true)}>
              {t("common.actions.edit")}
            </Button>
          ) : null}
          <Button variant="danger" onPress={() => setDeleting(true)}>
            {t("common.actions.delete")}
          </Button>
        </div>
      </header>
      {editing ? (
        <ServiceTypeEditor
          initialValue={{
            name: item.name,
            description: item.description ?? "",
          }}
          isPending={update.isPending}
          failure={
            update.isError ? t("clinic.serviceTypes.saveFailed") : undefined
          }
          onCancel={() => setEditing(false)}
          onSubmit={(value) =>
            update.mutate(
              {
                id: serviceTypeId,
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
      <ServiceTypeDeleteDialog
        isOpen={deleting}
        isPending={deletion.isPending}
        onCancel={() => setDeleting(false)}
        onConfirm={() =>
          deletion.mutate(serviceTypeId, {
            onSuccess: () => onNavigate({ kind: "serviceTypeIndex" }),
          })
        }
      />
    </section>
  );
}

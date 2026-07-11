"use client";

import { use, useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { Button, Input, Label, TextField } from "@heroui/react";
import { Widget } from "@heroui-pro/react";
import { Routes } from "@/app/routes";
import { ClinicDetailGrid, type ClinicDetailItem } from "@vitarish/clinic-web";
import { ClinicErrorState, ClinicLoadingState } from "@vitarish/clinic-web";
import { useServiceProviderDetail, useUpdateServiceProvider, useDeleteServiceProvider } from "@vitarish/clinic-web";
import { useLocalizedText } from "@/localization";

export default function ClinicServiceProviderDetailPage({
  params,
}: {
  params: Promise<{ serviceProviderId: string }>;
}) {
  const { serviceProviderId } = use(params);
  const router = useRouter();
  const { t } = useLocalizedText();
  const { data: provider, isLoading, error } = useServiceProviderDetail(serviceProviderId);
  const updateProvider = useUpdateServiceProvider();
  const deleteProvider = useDeleteServiceProvider();

  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [editing, setEditing] = useState(false);

  useEffect(() => {
    if (provider) {
      setName(provider.name);
      setDescription(provider.description ?? "");
    }
  }, [provider]);

  const handleSave = useCallback(() => {
    updateProvider.mutate(
      { id: serviceProviderId, name, description: description || undefined },
      { onSuccess: () => setEditing(false) }
    );
  }, [serviceProviderId, name, description, updateProvider]);

  const handleDelete = useCallback(() => {
    if (confirm(t("clinic.serviceProviders.confirmDelete"))) {
      deleteProvider.mutate(serviceProviderId, {
        onSuccess: () => router.push(Routes.Clinic.Appointments.ServiceProviders.Index),
      });
    }
  }, [serviceProviderId, deleteProvider, router, t]);

  if (isLoading) {
    return <ClinicLoadingState label={t("common.states.loading")} />;
  }

  if (error || !provider) {
    return <ClinicErrorState title={t("clinic.serviceProviders.notFound")} />;
  }

  const details: ClinicDetailItem[] = [
    { label: t("clinic.fields.name"), value: provider.name },
    { label: t("clinic.fields.description"), value: provider.description ?? t("common.values.notAvailable") },
    { label: t("clinic.fields.linkedUsers"), value: provider.linkedUserIds.length },
    { label: t("clinic.fields.created"), value: new Date(provider.createdAt).toLocaleString() },
    ...(provider.updatedAt
      ? [{ label: t("clinic.fields.updated"), value: new Date(provider.updatedAt).toLocaleString() }]
      : []),
  ];

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <div>
          <Button variant="ghost" className="mb-2" onPress={() => router.push(Routes.Clinic.Appointments.ServiceProviders.Index)}>
            <i aria-hidden className="fa-solid fa-arrow-left" />
            {t("common.actions.previous")}
          </Button>
          <h1 className="text-2xl font-bold">{editing ? t("clinic.serviceProviders.editTitle") : provider.name}</h1>
        </div>
        <div className="flex gap-2">
          {!editing && (
            <Button variant="ghost" onPress={() => setEditing(true)}>
              {t("common.actions.edit")}
            </Button>
          )}
          <Button variant="danger" isPending={deleteProvider.isPending} onPress={handleDelete}>
            {t("common.actions.delete")}
          </Button>
        </div>
      </div>

      {editing ? (
        <Widget>
          <Widget.Content className="flex flex-col gap-4">
            <TextField>
              <Label>{t("clinic.fields.name")}</Label>
              <Input value={name} onChange={(event) => setName(event.target.value)} />
            </TextField>
            <TextField>
              <Label>{t("clinic.fields.description")}</Label>
              <Input value={description} onChange={(event) => setDescription(event.target.value)} />
            </TextField>
            <div className="flex justify-end gap-2">
              <Button variant="ghost" onPress={() => setEditing(false)}>{t("common.actions.cancel")}</Button>
              <Button variant="primary" isPending={updateProvider.isPending} onPress={handleSave}>
                {t("common.actions.save")}
              </Button>
            </div>
          </Widget.Content>
        </Widget>
      ) : (
        <Widget>
          <Widget.Content>
            <ClinicDetailGrid items={details} />
          </Widget.Content>
        </Widget>
      )}
    </div>
  );
}

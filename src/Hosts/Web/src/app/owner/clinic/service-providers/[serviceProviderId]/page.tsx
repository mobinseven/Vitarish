"use client";

import { use, useCallback, useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import { Card, Button, TextField, Label, Input, Spinner } from "@heroui/react";
import { Routes } from "@/app/routes";
import { useServiceProviderDetail, useUpdateServiceProvider, useDeleteServiceProvider } from "@/features/clinic/service-providers/hooks/useServiceProviders";
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
    return (
      <Card className="p-8">
        <div className="flex items-center justify-center gap-2">
          <Spinner size="sm" />
          <span className="text-muted">{t("common.states.loading")}</span>
        </div>
      </Card>
    );
  }

  if (error || !provider) {
    return (
      <Card className="p-8">
        <p className="text-danger">{error?.message ?? t("clinic.serviceProviders.notFound")}</p>
      </Card>
    );
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <div>
          <Button variant="ghost" className="mb-2" onPress={() => router.push(Routes.Clinic.Appointments.ServiceProviders.Index)}>
            ← {t("common.actions.previous")}
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
        <Card>
          <Card.Content className="flex flex-col gap-4">
            <TextField>
              <Label>{t("clinic.fields.name")}</Label>
              <Input value={name} onChange={(e) => setName(e.target.value)} />
            </TextField>
            <TextField>
              <Label>{t("clinic.fields.description")}</Label>
              <Input value={description} onChange={(e) => setDescription(e.target.value)} />
            </TextField>
            <div className="flex justify-end gap-2">
              <Button variant="ghost" onPress={() => setEditing(false)}>{t("common.actions.cancel")}</Button>
              <Button variant="primary" isPending={updateProvider.isPending} onPress={handleSave}>
                {t("common.actions.save")}
              </Button>
            </div>
          </Card.Content>
        </Card>
      ) : (
        <Card>
          <Card.Content className="grid gap-4 sm:grid-cols-2">
            <DetailField label={t("clinic.fields.name")} value={provider.name} />
            <DetailField label={t("clinic.fields.description")} value={provider.description ?? "—"} />
            <DetailField label={t("clinic.fields.linkedUsers")} value={String(provider.linkedUserIds.length)} />
            <DetailField label={t("clinic.fields.created")} value={new Date(provider.createdAt).toLocaleString()} />
            {provider.updatedAt && (
              <DetailField label={t("clinic.fields.updated")} value={new Date(provider.updatedAt).toLocaleString()} />
            )}
          </Card.Content>
        </Card>
      )}
    </div>
  );
}

function DetailField({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex flex-col gap-1">
      <span className="text-xs text-muted">{label}</span>
      <span className="text-sm">{value}</span>
    </div>
  );
}

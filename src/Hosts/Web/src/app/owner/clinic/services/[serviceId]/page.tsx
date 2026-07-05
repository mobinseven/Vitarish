"use client";

import { use, useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { Button, Input, Label, TextField } from "@heroui/react";
import { Widget } from "@heroui-pro/react";
import { Routes } from "@/app/routes";
import { ClinicDetailGrid, type ClinicDetailItem } from "@/features/clinic/components/ClinicDetailGrid";
import { ClinicErrorState, ClinicLoadingState } from "@/features/clinic/components/ClinicStateSurface";
import { useServiceDetail, useUpdateService, useDeleteService } from "@/features/clinic/services/hooks/useServices";
import { useLocalizedText } from "@/localization";

export default function ClinicServiceDetailPage({
  params,
}: {
  params: Promise<{ serviceId: string }>;
}) {
  const { serviceId } = use(params);
  const router = useRouter();
  const { t } = useLocalizedText();
  const { data: service, isLoading, error } = useServiceDetail(serviceId);
  const updateService = useUpdateService();
  const deleteService = useDeleteService();

  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [editing, setEditing] = useState(false);

  useEffect(() => {
    if (service) {
      setName(service.name);
      setDescription(service.description ?? "");
    }
  }, [service]);

  const handleSave = useCallback(() => {
    if (!service) return;
    updateService.mutate(
      { id: serviceId, name, description: description || undefined, serviceTypeId: service.serviceTypeId },
      { onSuccess: () => setEditing(false) }
    );
  }, [serviceId, name, description, service, updateService]);

  const handleDelete = useCallback(() => {
    if (confirm(t("clinic.services.confirmDelete"))) {
      deleteService.mutate(serviceId, {
        onSuccess: () => router.push(Routes.Clinic.Appointments.Services.Index),
      });
    }
  }, [serviceId, deleteService, router, t]);

  if (isLoading) {
    return <ClinicLoadingState label={t("common.states.loading")} />;
  }

  if (error || !service) {
    return <ClinicErrorState title={t("clinic.services.notFound")} />;
  }

  const details: ClinicDetailItem[] = [
    { label: t("clinic.fields.name"), value: service.name },
    { label: t("clinic.fields.description"), value: service.description ?? t("common.values.notAvailable") },
    { label: t("clinic.fields.serviceType"), value: service.serviceTypeName ?? t("common.values.notAvailable") },
    { label: t("clinic.fields.created"), value: new Date(service.createdAt).toLocaleString() },
    ...(service.updatedAt
      ? [{ label: t("clinic.fields.updated"), value: new Date(service.updatedAt).toLocaleString() }]
      : []),
  ];

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <div>
          <Button variant="ghost" className="mb-2" onPress={() => router.push(Routes.Clinic.Appointments.Services.Index)}>
            <i aria-hidden className="fa-solid fa-arrow-left" />
            {t("common.actions.previous")}
          </Button>
          <h1 className="text-2xl font-bold">{editing ? t("clinic.services.editTitle") : service.name}</h1>
        </div>
        <div className="flex gap-2">
          {!editing && (
            <Button variant="ghost" onPress={() => setEditing(true)}>
              {t("common.actions.edit")}
            </Button>
          )}
          <Button variant="danger" isPending={deleteService.isPending} onPress={handleDelete}>
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
              <Button variant="primary" isPending={updateService.isPending} onPress={handleSave}>
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

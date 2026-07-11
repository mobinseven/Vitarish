"use client";

import { use, useCallback, useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { Button, Input, Label, TextField } from "@heroui/react";
import { Widget } from "@heroui-pro/react";
import { Routes } from "@/app/routes";
import { ClinicDetailGrid, type ClinicDetailItem } from "@vitarish/clinic-web";
import { ClinicErrorState, ClinicLoadingState } from "@vitarish/clinic-web";
import { useServiceTypeDetail, useUpdateServiceType, useDeleteServiceType } from "@vitarish/clinic-web";
import { useLocalizedText } from "@/localization";

export default function ClinicServiceTypeDetailPage({
  params,
}: {
  params: Promise<{ serviceTypeId: string }>;
}) {
  const { serviceTypeId } = use(params);
  const router = useRouter();
  const { t } = useLocalizedText();
  const { data: serviceType, isLoading, error } = useServiceTypeDetail(serviceTypeId);
  const updateType = useUpdateServiceType();
  const deleteType = useDeleteServiceType();

  const [name, setName] = useState("");
  const [description, setDescription] = useState("");
  const [editing, setEditing] = useState(false);

  useEffect(() => {
    if (serviceType) {
      setName(serviceType.name);
      setDescription(serviceType.description ?? "");
    }
  }, [serviceType]);

  const handleSave = useCallback(() => {
    updateType.mutate(
      { id: serviceTypeId, name, description: description || undefined },
      { onSuccess: () => setEditing(false) }
    );
  }, [serviceTypeId, name, description, updateType]);

  const handleDelete = useCallback(() => {
    if (confirm(t("clinic.serviceTypes.confirmDelete"))) {
      deleteType.mutate(serviceTypeId, {
        onSuccess: () => router.push(Routes.Clinic.Appointments.ServiceTypes.Index),
      });
    }
  }, [serviceTypeId, deleteType, router, t]);

  if (isLoading) {
    return <ClinicLoadingState label={t("common.states.loading")} />;
  }

  if (error || !serviceType) {
    return <ClinicErrorState title={t("clinic.serviceTypes.notFound")} />;
  }

  const details: ClinicDetailItem[] = [
    { label: t("clinic.fields.name"), value: serviceType.name },
    { label: t("clinic.fields.description"), value: serviceType.description ?? t("common.values.notAvailable") },
    { label: t("clinic.fields.created"), value: new Date(serviceType.createdAt).toLocaleString() },
    ...(serviceType.updatedAt
      ? [{ label: t("clinic.fields.updated"), value: new Date(serviceType.updatedAt).toLocaleString() }]
      : []),
  ];

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <div>
          <Button variant="ghost" className="mb-2" onPress={() => router.push(Routes.Clinic.Appointments.ServiceTypes.Index)}>
            <i aria-hidden className="fa-solid fa-arrow-left" />
            {t("common.actions.previous")}
          </Button>
          <h1 className="text-2xl font-bold">{editing ? t("clinic.serviceTypes.editTitle") : serviceType.name}</h1>
        </div>
        <div className="flex gap-2">
          {!editing && (
            <Button variant="ghost" onPress={() => setEditing(true)}>
              {t("common.actions.edit")}
            </Button>
          )}
          <Button variant="danger" isPending={deleteType.isPending} onPress={handleDelete}>
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
              <Button variant="primary" isPending={updateType.isPending} onPress={handleSave}>
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

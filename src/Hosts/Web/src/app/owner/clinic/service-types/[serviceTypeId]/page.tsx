"use client";

import { use, useCallback, useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import { Card, Button, TextField, Label, Input, Spinner } from "@heroui/react";
import { Routes } from "@/app/routes";
import { useServiceTypeDetail, useUpdateServiceType, useDeleteServiceType } from "@/features/clinic/service-types/hooks/useServiceTypes";
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
    if (confirm("Delete this service type?")) {
      deleteType.mutate(serviceTypeId, {
        onSuccess: () => router.push(Routes.Clinic.Appointments.ServiceTypes.Index),
      });
    }
  }, [serviceTypeId, deleteType, router]);

  if (isLoading) {
    return (
      <Card className="p-8">
        <div className="flex items-center justify-center gap-2">
          <Spinner size="sm" />
          <span className="text-default-500">{t("common.loading")}</span>
        </div>
      </Card>
    );
  }

  if (error || !serviceType) {
    return (
      <Card className="p-8">
        <p className="text-danger">{error?.message ?? "Service type not found."}</p>
      </Card>
    );
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <div>
          <Button variant="ghost" className="mb-2" onPress={() => router.push(Routes.Clinic.Appointments.ServiceTypes.Index)}>
            ← {t("common.actions.back")}
          </Button>
          <h1 className="text-2xl font-bold">{editing ? "Edit Service Type" : serviceType.name}</h1>
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
        <Card>
          <Card.Content className="flex flex-col gap-4">
            <TextField>
              <Label>Name</Label>
              <Input value={name} onChange={(e) => setName(e.target.value)} />
            </TextField>
            <TextField>
              <Label>Description</Label>
              <Input value={description} onChange={(e) => setDescription(e.target.value)} />
            </TextField>
            <div className="flex justify-end gap-2">
              <Button variant="ghost" onPress={() => setEditing(false)}>Cancel</Button>
              <Button variant="primary" isPending={updateType.isPending} onPress={handleSave}>
                {t("common.actions.save")}
              </Button>
            </div>
          </Card.Content>
        </Card>
      ) : (
        <Card>
          <Card.Content className="grid gap-4 sm:grid-cols-2">
            <DetailField label="Name" value={serviceType.name} />
            <DetailField label="Description" value={serviceType.description ?? "—"} />
            <DetailField label="Created" value={new Date(serviceType.createdAt).toLocaleString()} />
            {serviceType.updatedAt && (
              <DetailField label="Updated" value={new Date(serviceType.updatedAt).toLocaleString()} />
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
      <span className="text-xs text-default-500">{label}</span>
      <span className="text-sm">{value}</span>
    </div>
  );
}

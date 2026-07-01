"use client";

import { use, useCallback, useState, useEffect } from "react";
import { useRouter } from "next/navigation";
import { Card, Button, TextField, Label, Input, Spinner } from "@heroui/react";
import { Routes } from "@/app/routes";
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
    if (confirm("Delete this service?")) {
      deleteService.mutate(serviceId, {
        onSuccess: () => router.push(Routes.Clinic.Appointments.Services.Index),
      });
    }
  }, [serviceId, deleteService, router]);

  if (isLoading) {
    return (
      <Card className="p-8">
        <div className="flex items-center justify-center gap-2">
          <Spinner size="sm" />
          <span className="text-default-500">{t("common.states.loading")}</span>
        </div>
      </Card>
    );
  }

  if (error || !service) {
    return (
      <Card className="p-8">
        <p className="text-danger">{error?.message ?? "Service not found."}</p>
      </Card>
    );
  }

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <div>
          <Button variant="ghost" className="mb-2" onPress={() => router.push(Routes.Clinic.Appointments.Services.Index)}>
            ← {t("common.actions.previous")}
          </Button>
          <h1 className="text-2xl font-bold">{editing ? "Edit Service" : service.name}</h1>
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
              <Button variant="primary" isPending={updateService.isPending} onPress={handleSave}>
                {t("common.actions.save")}
              </Button>
            </div>
          </Card.Content>
        </Card>
      ) : (
        <Card>
          <Card.Content className="grid gap-4 sm:grid-cols-2">
            <DetailField label="Name" value={service.name} />
            <DetailField label="Description" value={service.description ?? "—"} />
            <DetailField label="Service Type" value={service.serviceTypeName ?? "—"} />
            <DetailField label="Created" value={new Date(service.createdAt).toLocaleString()} />
            {service.updatedAt && (
              <DetailField label="Updated" value={new Date(service.updatedAt).toLocaleString()} />
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

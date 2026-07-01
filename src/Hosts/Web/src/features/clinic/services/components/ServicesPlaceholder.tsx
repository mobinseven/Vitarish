"use client";

import { useCallback } from "react";
import { Card, Button, Spinner } from "@heroui/react";
import { useServices, useDeleteService } from "../hooks/useServices";
import { useLocalizedText } from "@/localization";

export function ServicesPlaceholder() {
  const { t } = useLocalizedText();
  const { data: services, isLoading, error } = useServices();
  const deleteService = useDeleteService();

  const handleDelete = useCallback(
    (id: string) => {
      if (confirm("Delete this service?")) {
        deleteService.mutate(id);
      }
    },
    [deleteService]
  );

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold">{t("clinic.services.title")}</h1>
        <p className="text-sm text-default-500">{t("clinic.services.description")}</p>
      </div>

      {isLoading && (
        <Card className="p-8">
          <div className="flex items-center justify-center gap-2">
            <Spinner size="sm" />
          </div>
        </Card>
      )}

      {error && (
        <Card className="p-8">
          <p className="text-sm text-danger">Failed to load services.</p>
        </Card>
      )}

      {!isLoading && !error && (
        <Card>
          <Card.Content className="flex flex-col gap-1">
            {(!services || services.length === 0) && (
              <p className="py-8 text-center text-sm text-default-500">No services found.</p>
            )}
            {services?.map((s) => (
              <div key={s.id} className="flex items-center justify-between rounded-lg px-4 py-3 transition-colors hover:bg-default-100">
                <div className="flex flex-col gap-1">
                  <span className="text-sm font-medium">{s.name}</span>
                  <span className="text-xs text-default-500">{s.serviceTypeName ?? "No type"}</span>
                </div>
                <Button variant="ghost" className="text-danger" onPress={() => handleDelete(s.id)}>
                  {t("common.actions.delete")}
                </Button>
              </div>
            ))}
          </Card.Content>
        </Card>
      )}
    </div>
  );
}

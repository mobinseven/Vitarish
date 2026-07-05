"use client";

import { useCallback } from "react";
import { Button } from "@heroui/react";
import { ListView } from "@heroui-pro/react";
import { ClinicEmptyState, ClinicErrorState, ClinicLoadingState } from "@/features/clinic/components/ClinicStateSurface";
import { useServices, useDeleteService } from "../hooks/useServices";
import { useLocalizedText } from "@/localization";

export function ServicesPlaceholder() {
  const { t } = useLocalizedText();
  const { data: services, isLoading, error } = useServices();
  const deleteService = useDeleteService();

  const handleDelete = useCallback(
    (id: string) => {
      if (confirm(t("clinic.services.confirmDelete"))) {
        deleteService.mutate(id);
      }
    },
    [deleteService, t]
  );

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold">{t("clinic.services.title")}</h1>
        <p className="text-sm text-muted">{t("clinic.services.description")}</p>
      </div>

      {isLoading && (
        <ClinicLoadingState label={t("common.states.loading")} />
      )}

      {error && (
        <ClinicErrorState title={t("clinic.services.failedToLoad")} />
      )}

      {!isLoading && !error && (
        <ListView
          aria-label={t("clinic.services.title")}
          renderEmptyState={() => <ClinicEmptyState title={t("clinic.services.empty")} />}
        >
          {services?.map((s) => (
            <ListView.Item key={s.id} id={s.id} textValue={s.name}>
              <ListView.ItemContent>
                <div className="flex flex-col gap-1">
                  <ListView.Title>{s.name}</ListView.Title>
                  <ListView.Description>{s.serviceTypeName ?? t("clinic.services.noType")}</ListView.Description>
                </div>
              </ListView.ItemContent>
              <ListView.ItemAction>
                <Button variant="ghost" className="text-danger" onPress={() => handleDelete(s.id)}>
                  {t("common.actions.delete")}
                </Button>
              </ListView.ItemAction>
            </ListView.Item>
          ))}
        </ListView>
      )}
    </div>
  );
}

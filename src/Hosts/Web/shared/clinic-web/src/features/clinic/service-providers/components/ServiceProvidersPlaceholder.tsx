"use client";

import { useCallback } from "react";
import { Button } from "@heroui/react";
import { ListView } from "@heroui-pro/react";
import { ClinicEmptyState, ClinicErrorState, ClinicLoadingState } from "../../../../features/clinic/components/ClinicStateSurface";
import { useServiceProviders, useDeleteServiceProvider } from "../hooks/useServiceProviders";
import { useLocalizedText } from "../../../../localization";

export function ServiceProvidersPlaceholder() {
  const { t } = useLocalizedText();
  const { data: providers, isLoading, error } = useServiceProviders();
  const deleteProvider = useDeleteServiceProvider();

  const handleDelete = useCallback(
    (id: string) => {
      if (confirm(t("clinic.serviceProviders.confirmDelete"))) {
        deleteProvider.mutate(id);
      }
    },
    [deleteProvider, t]
  );

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold">{t("clinic.serviceProviders.title")}</h1>
        <p className="text-sm text-muted">{t("clinic.serviceProviders.description")}</p>
      </div>

      {isLoading && (
        <ClinicLoadingState label={t("common.states.loading")} />
      )}

      {error && (
        <ClinicErrorState title={t("clinic.serviceProviders.failedToLoad")} />
      )}

      {!isLoading && !error && (
        <ListView
          aria-label={t("clinic.serviceProviders.title")}
          renderEmptyState={() => <ClinicEmptyState title={t("clinic.serviceProviders.empty")} />}
        >
          {providers?.map((sp) => (
            <ListView.Item key={sp.id} id={sp.id} textValue={sp.name}>
              <ListView.ItemContent>
                <div className="flex flex-col gap-1">
                  <ListView.Title>{sp.name}</ListView.Title>
                  <ListView.Description>
                    {t("clinic.serviceProviders.linkedUsers", { count: sp.linkedUserIds.length })}
                  </ListView.Description>
                </div>
              </ListView.ItemContent>
              <ListView.ItemAction>
                <Button variant="ghost" className="text-danger" onPress={() => handleDelete(sp.id)}>
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

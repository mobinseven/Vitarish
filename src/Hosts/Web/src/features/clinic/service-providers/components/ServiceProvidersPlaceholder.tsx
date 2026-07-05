"use client";

import { useCallback } from "react";
import { Card, Button, Spinner } from "@heroui/react";
import { useServiceProviders, useDeleteServiceProvider } from "../hooks/useServiceProviders";
import { useLocalizedText } from "@/localization";

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
        <Card className="p-8">
          <div className="flex items-center justify-center gap-2">
            <Spinner size="sm" />
          </div>
        </Card>
      )}

      {error && (
        <Card className="p-8">
          <p className="text-sm text-danger">{t("clinic.serviceProviders.failedToLoad")}</p>
        </Card>
      )}

      {!isLoading && !error && (
        <Card>
          <Card.Content className="flex flex-col gap-1">
            {(!providers || providers.length === 0) && (
              <p className="py-8 text-center text-sm text-muted">{t("clinic.serviceProviders.empty")}</p>
            )}
            {providers?.map((sp) => (
              <div key={sp.id} className="flex items-center justify-between rounded-lg px-4 py-3 transition-colors hover:bg-surface-secondary">
                <div className="flex flex-col gap-1">
                  <span className="text-sm font-medium">{sp.name}</span>
                  <span className="text-xs text-muted">{t("clinic.serviceProviders.linkedUsers", { count: sp.linkedUserIds.length })}</span>
                </div>
                <Button variant="ghost" className="text-danger" onPress={() => handleDelete(sp.id)}>
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

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
      if (confirm("Delete this service provider?")) {
        deleteProvider.mutate(id);
      }
    },
    [deleteProvider]
  );

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold">{t("clinic.serviceProviders.title")}</h1>
        <p className="text-sm text-default-500">{t("clinic.serviceProviders.description")}</p>
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
          <p className="text-sm text-danger">Failed to load service providers.</p>
        </Card>
      )}

      {!isLoading && !error && (
        <Card>
          <Card.Content className="flex flex-col gap-1">
            {(!providers || providers.length === 0) && (
              <p className="py-8 text-center text-sm text-default-500">No service providers found.</p>
            )}
            {providers?.map((sp) => (
              <div key={sp.id} className="flex items-center justify-between rounded-lg px-4 py-3 transition-colors hover:bg-default-100">
                <div className="flex flex-col gap-1">
                  <span className="text-sm font-medium">{sp.name}</span>
                  <span className="text-xs text-default-500">{sp.linkedUserIds.length} linked users</span>
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

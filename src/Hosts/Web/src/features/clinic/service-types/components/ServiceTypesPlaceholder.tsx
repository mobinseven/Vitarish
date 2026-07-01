"use client";

import { useCallback } from "react";
import { useRouter } from "next/navigation";
import { Card, Button, Spinner } from "@heroui/react";
import { Routes } from "@/app/routes";
import { useServiceTypes, useDeleteServiceType } from "../hooks/useServiceTypes";
import { useLocalizedText } from "@/localization";

export function ServiceTypesPlaceholder() {
  const router = useRouter();
  const { t } = useLocalizedText();
  const { data: types, isLoading, error } = useServiceTypes();
  const deleteType = useDeleteServiceType();

  const handleDelete = useCallback(
    (id: string) => {
      if (confirm("Delete this service type?")) {
        deleteType.mutate(id);
      }
    },
    [deleteType]
  );

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold">{t("clinic.serviceTypes.title")}</h1>
        <p className="text-sm text-default-500">{t("clinic.serviceTypes.description")}</p>
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
          <p className="text-sm text-danger">Failed to load service types.</p>
        </Card>
      )}

      {!isLoading && !error && (
        <Card>
          <Card.Content className="flex flex-col gap-1">
            {(!types || types.length === 0) && (
              <p className="py-8 text-center text-sm text-default-500">No service types found.</p>
            )}
            {types?.map((st) => (
              <div key={st.id} className="flex items-center justify-between rounded-lg px-4 py-3 transition-colors hover:bg-default-100">
                <div className="flex flex-col gap-1">
                  <span className="text-sm font-medium">{st.name}</span>
                  {st.description && <span className="text-xs text-default-500">{st.description}</span>}
                </div>
                <Button variant="ghost" className="text-danger" onPress={() => handleDelete(st.id)}>
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

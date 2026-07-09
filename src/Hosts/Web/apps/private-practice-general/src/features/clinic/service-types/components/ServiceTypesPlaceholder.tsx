"use client";

import { useCallback } from "react";
import { Button } from "@heroui/react";
import { ListView } from "@heroui-pro/react";
import { ClinicEmptyState, ClinicErrorState, ClinicLoadingState } from "@/features/clinic/components/ClinicStateSurface";
import { useServiceTypes, useDeleteServiceType } from "../hooks/useServiceTypes";
import { useLocalizedText } from "@/localization";

export function ServiceTypesList() {
  const { t } = useLocalizedText();
  const { data: types, isLoading, error } = useServiceTypes();
  const deleteType = useDeleteServiceType();

  const handleDelete = useCallback(
    (id: string) => {
      if (confirm(t("clinic.serviceTypes.confirmDelete"))) {
        deleteType.mutate(id);
      }
    },
    [deleteType, t]
  );

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold">{t("clinic.serviceTypes.title")}</h1>
        <p className="text-sm text-muted">{t("clinic.serviceTypes.description")}</p>
      </div>

      {isLoading && (
        <ClinicLoadingState label={t("common.states.loading")} />
      )}

      {error && (
        <ClinicErrorState title={t("clinic.serviceTypes.failedToLoad")} />
      )}

      {!isLoading && !error && (
        <ListView
          aria-label={t("clinic.serviceTypes.title")}
          renderEmptyState={() => <ClinicEmptyState title={t("clinic.serviceTypes.empty")} />}
        >
          {types?.map((st) => (
            <ListView.Item key={st.id} id={st.id} textValue={st.name}>
              <ListView.ItemContent>
                <div className="flex flex-col gap-1">
                  <ListView.Title>{st.name}</ListView.Title>
                  {st.description && <ListView.Description>{st.description}</ListView.Description>}
                </div>
              </ListView.ItemContent>
              <ListView.ItemAction>
                <Button variant="ghost" className="text-danger" onPress={() => handleDelete(st.id)}>
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

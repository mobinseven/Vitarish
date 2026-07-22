"use client";

import { useCallback } from "react";
import {
  DataGrid,
  type DataGridColumn,
  KPI,
  KPIGroup,
  Widget,
} from "@heroui-pro/react";
import type { SeaNavigationIntent } from "../../navigation";
import { SeaLoadingState, SeaEmptyState } from "../state/SeaStateSurface";
import { useSeaProcedures } from "../../hooks/procedures/useSeaProcedures";
import { useLocalizedText } from "../../localization";
import type { SeaProcedureReadDto } from "../../models/seaStatModels";

interface SeaProcedureListProps {
  projectId: string;
  onNavigate?: (intent: SeaNavigationIntent) => void;
}

export function SeaProcedureList({
  projectId,
  onNavigate,
}: SeaProcedureListProps) {
  const { t } = useLocalizedText();
  const { data: procedures, isLoading } = useSeaProcedures(projectId);

  const handleSelectProcedure = useCallback(
    (procedureId: string) => {
      onNavigate?.({ kind: "procedureDetail", projectId, procedureId });
    },
    [onNavigate, projectId],
  );

  const columns: DataGridColumn<SeaProcedureReadDto>[] = [
    {
      id: "name",
      header: t("sea.procedures.name"),
      accessorKey: "name",
      isRowHeader: true,
      allowsSorting: true,
      minWidth: 200,
      cell: (item) => (
        <div className="min-w-0">
          <div className="text-sm font-medium truncate">{item.name}</div>
          {item.description && (
            <div className="text-xs text-muted truncate">
              {item.description}
            </div>
          )}
        </div>
      ),
    },
    {
      id: "stats",
      header: t("sea.projects.stats"),
      minWidth: 160,
      cell: (item) => {
        if (!item.stats) return <span className="text-xs text-muted">-</span>;
        return (
          <KPIGroup orientation="horizontal">
            <KPI>
              <KPI.Content>
                <KPI.Value
                  maximumFractionDigits={0}
                  value={Number(item.stats.events) || 0}
                />
              </KPI.Content>
              <KPI.Header>
                <KPI.Title>{t("sea.stats.events")}</KPI.Title>
              </KPI.Header>
            </KPI>
            {item.stats.newItems > 0 && (
              <KPI>
                <KPI.Content>
                  <KPI.Value
                    maximumFractionDigits={0}
                    value={Number(item.stats.newItems)}
                  />
                </KPI.Content>
                <KPI.Header>
                  <KPI.Title className="text-warning">
                    {t("sea.stats.newItems")}
                  </KPI.Title>
                </KPI.Header>
              </KPI>
            )}
          </KPIGroup>
        );
      },
    },
  ];

  if (isLoading) {
    return <SeaLoadingState label={t("sea.procedures.loading")} />;
  }

  const procedureList = procedures ?? [];

  return (
    <Widget>
      <Widget.Header>
        <Widget.Title>{t("sea.procedures.title")}</Widget.Title>
      </Widget.Header>
      <Widget.Content>
        {procedureList.length === 0 ? (
          <SeaEmptyState title={t("sea.procedures.emptyState")} />
        ) : (
          <DataGrid
            aria-label={t("sea.procedures.title")}
            columns={columns}
            data={procedureList}
            getRowId={(item) => item.id}
            variant="primary"
            onRowAction={(key) => handleSelectProcedure(String(key))}
          />
        )}
      </Widget.Content>
    </Widget>
  );
}

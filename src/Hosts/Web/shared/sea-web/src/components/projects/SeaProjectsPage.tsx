"use client";

import { useCallback } from "react";
import {
  DataGrid,
  type DataGridColumn,
  KPI,
  KPIGroup,
} from "@heroui-pro/react";
import type { SeaNavigationIntent } from "../../navigation";
import {
  SeaLoadingState,
  SeaErrorState,
  SeaEmptyState,
} from "../state/SeaStateSurface";
import { useSeaProjects } from "../../hooks/projects/useSeaProjects";
import { useLocalizedText, formatDateTime } from "../../localization";
import type { SeaProjectReadDto } from "../../models/seaStatModels";
import { SeaProjectForm } from "./SeaProjectForm";
import { useState } from "react";
import { Button } from "@heroui/react";

interface SeaProjectsPageProps {
  onNavigate?: (intent: SeaNavigationIntent) => void;
}

export function SeaProjectsPage({ onNavigate }: SeaProjectsPageProps) {
  const { t, locale } = useLocalizedText();
  const { data: projects, isLoading, error } = useSeaProjects();
  const [isCreateOpen, setIsCreateOpen] = useState(false);

  const handleSelectProject = useCallback(
    (projectId: string) => {
      onNavigate?.({ kind: "projectDetail", projectId });
    },
    [onNavigate],
  );

  const columns: DataGridColumn<SeaProjectReadDto>[] = [
    {
      id: "name",
      header: t("sea.projects.name"),
      accessorKey: "name",
      isRowHeader: true,
      allowsSorting: true,
      minWidth: 200,
      cell: (item) => (
        <div className="flex items-center gap-2">
          <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-accent/10 text-accent text-sm font-medium">
            {item.name?.charAt(0)?.toUpperCase() ?? "?"}
          </div>
          <div className="min-w-0">
            <div className="text-sm font-medium truncate">{item.name}</div>
            {item.description && (
              <div className="text-xs text-muted truncate">
                {item.description}
              </div>
            )}
          </div>
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
                  value={Number(item.stats.procedures) || 0}
                />
              </KPI.Content>
              <KPI.Header>
                <KPI.Title>{t("sea.stats.procedures")}</KPI.Title>
              </KPI.Header>
            </KPI>
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
    {
      id: "updatedAt",
      header: t("sea.projects.updatedAt"),
      accessorKey: "updatedAt",
      allowsSorting: true,
      minWidth: 140,
      cell: (item) => (
        <span className="text-xs text-muted">
          {item.updatedAt ? formatDateTime(item.updatedAt, locale) : "-"}
        </span>
      ),
    },
  ];

  if (isLoading) {
    return <SeaLoadingState label={t("sea.projects.loading")} />;
  }

  if (error) {
    return <SeaErrorState title={t("sea.errors.notFound")} />;
  }

  const projectList = projects ?? [];

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl font-bold">{t("sea.projects.title")}</h1>
        <Button variant="primary" onPress={() => setIsCreateOpen(true)}>
          {t("sea.projects.newProject")}
        </Button>
      </div>

      {projectList.length === 0 ? (
        <SeaEmptyState
          title={t("sea.projects.emptyState")}
          description={t("sea.projects.newProject")}
        >
          <Button variant="primary" onPress={() => setIsCreateOpen(true)}>
            {t("sea.projects.newProject")}
          </Button>
        </SeaEmptyState>
      ) : (
        <DataGrid
          aria-label={t("sea.projects.title")}
          columns={columns}
          data={projectList}
          getRowId={(item) => item.id}
          variant="primary"
          onRowAction={(key) => handleSelectProject(String(key))}
        />
      )}

      <SeaProjectForm
        isOpen={isCreateOpen}
        onClose={() => setIsCreateOpen(false)}
        onNavigate={onNavigate}
      />
    </div>
  );
}

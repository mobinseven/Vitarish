"use client";

import { useCallback } from "react";
import { useRouter } from "next/navigation";
import { DataGrid, type DataGridColumn } from "@heroui-pro/react";
import { Routes } from "@/app/routes";
import { SeaLoadingState, SeaEmptyState } from "../../shared/components/SeaStateSurface";
import { useSeaTemplateProjects } from "../hooks/useSeaTemplates";
import { useLocalizedText, formatDateTime } from "@/localization";
import type { SeaProjectReadDto } from "../../shared/models/seaStatModels";

export function SeaTemplateProjectsPage() {
  const router = useRouter();
  const { t, locale } = useLocalizedText();
  const { data: templates, isLoading } = useSeaTemplateProjects();

  const handleSelectTemplate = useCallback(
    (projectId: string) => {
      router.push(Routes.Sea.Templates.Detail(projectId));
    },
    [router]
  );

  const columns: DataGridColumn<SeaProjectReadDto>[] = [
    {
      id: "name",
      header: t("sea.projects.name"),
      accessorKey: "name",
      isRowHeader: true,
      allowsSorting: true,
      minWidth: 200,
    },
    {
      id: "createdAt",
      header: t("sea.projects.createdAt"),
      accessorKey: "createdAt",
      allowsSorting: true,
      minWidth: 140,
      cell: (item) => (
        <span className="text-xs text-muted">
          {item.createdAt ? formatDateTime(item.createdAt, locale) : "-"}
        </span>
      ),
    },
  ];

  if (isLoading) {
    return <SeaLoadingState label={t("sea.templates.loading")} />;
  }

  const templateList = (templates ?? []).filter((p) => p.template);

  if (templateList.length === 0) {
    return <SeaEmptyState title={t("sea.templates.emptyState")} />;
  }

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl font-bold">{t("sea.templates.title")}</h1>
      <DataGrid
        aria-label={t("sea.templates.title")}
        columns={columns}
        data={templateList}
        getRowId={(item) => item.id}
        variant="primary"
        onRowAction={(key) => handleSelectTemplate(String(key))}
      />
    </div>
  );
}

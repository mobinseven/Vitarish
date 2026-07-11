"use client";
import { DetailGrid, type DetailGridItem } from "@vitarish/web-ui";

import { useState } from "react";
import { Button } from "@heroui/react";
import { Widget, KPI, KPIGroup } from "@heroui-pro/react";
import type { SeaNavigationIntent } from "../../../../navigation";
import { SeaLoadingState, SeaErrorState } from "../../shared/components/SeaStateSurface";
import { useSeaProjectDetail } from "../hooks/useSeaProjectDetail";
import { useLocalizedText, formatDateTime } from "../../../../localization";
import { SeaProjectDeleteDialog } from "./SeaProjectDeleteDialog";

interface SeaProjectDetailPageProps {
  projectId: string;
  onNavigate?: (intent: SeaNavigationIntent) => void;
}

export function SeaProjectDetailPage({ projectId, onNavigate }: SeaProjectDetailPageProps) {
  const { t, locale } = useLocalizedText();
  const { data: project, isLoading, error } = useSeaProjectDetail(projectId);
  const [isDeleteOpen, setIsDeleteOpen] = useState(false);

  if (isLoading) {
    return <SeaLoadingState label={t("sea.projects.loading")} />;
  }

  if (error || !project) {
    return <SeaErrorState title={t("sea.projects.notFound")} />;
  }

  const details: DetailGridItem[] = [
    { label: t("sea.projects.name"), value: project.name },
    ...(project.description ? [{ label: t("sea.projects.description"), value: project.description }] : []),
    { label: t("sea.projects.createdAt"), value: formatDateTime(project.createdAt, locale) },
    ...(project.updatedAt ? [{ label: t("sea.projects.updatedAt"), value: formatDateTime(project.updatedAt, locale) }] : []),
  ];

  return (
    <div className="flex flex-col gap-6">
      <div className="flex items-center justify-between">
        <div>
          <Button
            variant="ghost"
            className="mb-2"
            onPress={() => onNavigate?.({ kind: "projectList" })}
          >
            {t("sea.projects.backToProjects")}
          </Button>
          <h1 className="text-2xl font-bold">{project.name}</h1>
          {project.description && (
            <p className="text-sm text-muted">{project.description}</p>
          )}
        </div>
        <div className="flex gap-2">
          <Button
            variant="secondary"
            onPress={() => onNavigate?.({ kind: "projectEdit", projectId })}
          >
            {t("sea.projects.editProject")}
          </Button>
          <Button
            variant="danger"
            onPress={() => setIsDeleteOpen(true)}
          >
            {t("sea.projects.deleteProject")}
          </Button>
        </div>
      </div>

      <Widget>
        <Widget.Header>
          <Widget.Title>{t("sea.projects.stats")}</Widget.Title>
        </Widget.Header>
        <Widget.Content>
          {project.stats ? (
            <KPIGroup orientation="horizontal">
              <KPI>
                <KPI.Content>
                  <KPI.Value maximumFractionDigits={0} value={Number(project.stats.procedures) || 0} />
                </KPI.Content>
                <KPI.Header>
                  <KPI.Title>{t("sea.stats.procedures")}</KPI.Title>
                </KPI.Header>
              </KPI>
              <KPI>
                <KPI.Content>
                  <KPI.Value maximumFractionDigits={0} value={Number(project.stats.events) || 0} />
                </KPI.Content>
                <KPI.Header>
                  <KPI.Title>{t("sea.stats.events")}</KPI.Title>
                </KPI.Header>
              </KPI>
              <KPI>
                <KPI.Content>
                  <KPI.Value maximumFractionDigits={0} value={Number(project.stats.committedEvents) || 0} />
                </KPI.Content>
                <KPI.Header>
                  <KPI.Title>{t("sea.stats.committedEvents")}</KPI.Title>
                </KPI.Header>
              </KPI>
              <KPI>
                <KPI.Content>
                  <KPI.Value maximumFractionDigits={0} value={Number(project.stats.eventItems) || 0} />
                </KPI.Content>
                <KPI.Header>
                  <KPI.Title>{t("sea.stats.eventItems")}</KPI.Title>
                </KPI.Header>
              </KPI>
              {project.stats.newItems > 0 && (
                <KPI>
                  <KPI.Content>
                    <KPI.Value maximumFractionDigits={0} value={Number(project.stats.newItems)} />
                  </KPI.Content>
                  <KPI.Header>
                    <KPI.Title className="text-warning">{t("sea.stats.newItems")}</KPI.Title>
                  </KPI.Header>
                </KPI>
              )}
            </KPIGroup>
          ) : (
            <p className="text-sm text-muted">{t("sea.stats.emptyState")}</p>
          )}
        </Widget.Content>
      </Widget>

      <Widget>
        <Widget.Header>
          <Widget.Title>{t("sea.projects.description")}</Widget.Title>
        </Widget.Header>
        <Widget.Content>
          <DetailGrid items={details} />
        </Widget.Content>
      </Widget>

      <SeaProjectDeleteDialog
        isOpen={isDeleteOpen}
        onClose={() => setIsDeleteOpen(false)}
        onDeleted={() => onNavigate?.({ kind: "projectList" })}
        project={project}
      />
    </div>
  );
}

"use client";

import { Button, Chip, Tooltip } from "@heroui/react";
import { Widget } from "@heroui-pro/react";
import { KPI } from "@heroui-pro/react/kpi";
import { KPIGroup } from "@heroui-pro/react/kpi-group";
import { useRouter } from "next/navigation";
import { useLocalizedText } from "@vitarish/web-localization";
import { useSeaProcedures } from "@vitarish/sea-web";
import { useSeaProjectDetail } from "@vitarish/sea-web";
import { SeaErrorState, SeaLoadingState } from "@vitarish/sea-web";
import { Routes } from "@/app/routes";
import { FaIcon } from "@vitarish/web-ui";

export function PmProjectOverview({ projectId }: { projectId: string }) {
  const router = useRouter();
  const { t } = useLocalizedText();
  const projectQuery = useSeaProjectDetail(projectId);
  const proceduresQuery = useSeaProcedures(projectId);

  if (projectQuery.isLoading || proceduresQuery.isLoading) {
    return <SeaLoadingState label={t("sea.projects.loading")} />;
  }
  if (projectQuery.error || !projectQuery.data) {
    return <SeaErrorState title={t("sea.projects.notFound")} />;
  }

  const project = projectQuery.data;
  const stats = project.stats;
  const procedures = proceduresQuery.data ?? [];

  return (
    <div className="flex flex-col gap-5">
      <header className="flex flex-col gap-4 border-b border-border/70 pb-5 sm:flex-row sm:items-start sm:justify-between">
        <div className="min-w-0">
          <Tooltip>
            <Button
              variant="ghost"
              size="sm"
              onPress={() => router.push(Routes.Projects)}
            >
              <FaIcon icon="arrow-left" aria-hidden="true" />
              {t("sea.projects.backToProjects")}
            </Button>
            <Tooltip.Content>
              {t("sea.projects.backToProjects")}
            </Tooltip.Content>
          </Tooltip>
          <p className="mt-3 text-xs font-semibold uppercase tracking-[0.16em] text-muted">
            {t("pm.projectWorkspace")}
          </p>
          <h1 className="mt-1 text-3xl font-semibold tracking-tight">
            {project.name}
          </h1>
          {project.description && (
            <p className="mt-2 max-w-2xl text-sm leading-6 text-muted">
              {project.description}
            </p>
          )}
        </div>
        <Tooltip>
          <Button
            variant="secondary"
            onPress={() => router.push(Routes.ProjectEdit(projectId))}
          >
            <FaIcon icon="pen" aria-hidden="true" />
            {t("sea.projects.editProject")}
          </Button>
          <Tooltip.Content>{t("sea.projects.editProject")}</Tooltip.Content>
        </Tooltip>
      </header>

      <section
        aria-labelledby="project-statistics-title"
        className="rounded-xl bg-surface-secondary/70 p-4 sm:p-5"
      >
        <div className="mb-3 flex items-center justify-between">
          <h2 id="project-statistics-title" className="text-sm font-semibold">
            {t("sea.projects.stats")}
          </h2>
          <span className="text-xs text-muted">
            {procedures.length} {t("sea.stats.procedures")}
          </span>
        </div>
        {stats ? (
          <KPIGroup
            orientation="horizontal"
            className="grid grid-cols-2 gap-x-5 gap-y-4 sm:grid-cols-5"
          >
            <Metric
              label={t("sea.stats.procedures")}
              value={stats.procedures}
            />
            <Metric label={t("sea.stats.events")} value={stats.events} />
            <Metric
              label={t("sea.stats.committedEvents")}
              value={stats.committedEvents}
              tone="success"
            />
            <Metric
              label={t("sea.stats.eventItems")}
              value={stats.eventItems}
            />
            <Metric
              label={t("sea.stats.newItems")}
              value={stats.newItems}
              tone={stats.newItems > 0 ? "warning" : undefined}
            />
          </KPIGroup>
        ) : (
          <p className="text-sm text-muted">{t("sea.stats.emptyState")}</p>
        )}
      </section>

      <section
        className="flex flex-col gap-3"
        aria-labelledby="procedure-list-title"
      >
        <div className="flex items-end justify-between gap-3">
          <div>
            <p className="text-xs font-semibold uppercase tracking-[0.16em] text-muted">
              {t("pm.workstream")}
            </p>
            <h2
              id="procedure-list-title"
              className="mt-1 text-xl font-semibold"
            >
              {t("sea.procedures.title")}
            </h2>
          </div>
          <span className="text-sm text-muted">{procedures.length}</span>
        </div>
        {procedures.length === 0 ? (
          <Widget>
            <Widget.Content>
              <p className="text-sm text-muted">
                {t("sea.procedures.emptyState")}
              </p>
            </Widget.Content>
          </Widget>
        ) : (
          procedures.map((procedure) => {
            const procedureStats = procedure.stats;
            const hasUnread =
              procedureStats?.unread || (procedureStats?.newItems ?? 0) > 0;
            return (
              <Widget
                key={procedure.id}
                className={
                  hasUnread
                    ? "border-warning/25 bg-warning/[0.045]"
                    : "bg-content1"
                }
              >
                <Widget.Content className="grid gap-4 p-4 md:grid-cols-[minmax(0,1fr)_auto] md:items-center">
                  <div className="min-w-0">
                    <div className="flex flex-wrap items-center gap-2">
                      <h3 className="truncate text-base font-semibold">
                        {procedure.name}
                      </h3>
                      {hasUnread && (
                        <Chip size="sm" color="warning" variant="soft">
                          {t("pm.needsAttention")}
                        </Chip>
                      )}
                    </div>
                    {procedure.description && (
                      <p className="mt-1 line-clamp-2 text-sm text-muted">
                        {procedure.description}
                      </p>
                    )}
                  </div>
                  <div className="grid grid-cols-[minmax(4rem,auto)_minmax(7rem,auto)_auto] items-center gap-3 text-sm sm:gap-5">
                    <div>
                      <p className="text-xs text-muted">
                        {t("sea.stats.events")}
                      </p>
                      <p className="font-semibold tabular-nums">
                        {procedureStats?.events ?? 0}
                      </p>
                    </div>
                    <div>
                      <p className="text-xs text-muted">
                        {t("sea.stats.committedEvents")}
                      </p>
                      <p className="font-semibold tabular-nums text-success">
                        {procedureStats?.committedEvents ?? 0}
                      </p>
                    </div>
                    <Tooltip>
                      <Button
                        size="sm"
                        variant="secondary"
                        aria-label={`${t("common.actions.view")} ${procedure.name}`}
                        onPress={() =>
                          router.push(Routes.Procedure(projectId, procedure.id))
                        }
                      >
                        <FaIcon icon="arrow-right" aria-hidden="true" />
                        <span className="hidden sm:inline">
                          {t("common.actions.view")}
                        </span>
                      </Button>
                      <Tooltip.Content>
                        {t("common.actions.view")}
                      </Tooltip.Content>
                    </Tooltip>
                  </div>
                </Widget.Content>
              </Widget>
            );
          })
        )}
      </section>
    </div>
  );
}

function Metric({
  label,
  value,
  tone,
}: {
  label: string;
  value: number;
  tone?: "success" | "warning";
}) {
  return (
    <KPI>
      <KPI.Content>
        <KPI.Value
          className={`tabular-nums ${tone === "success" ? "text-success" : tone === "warning" ? "text-warning" : ""}`}
          maximumFractionDigits={0}
          value={Number(value) || 0}
        />
      </KPI.Content>
      <KPI.Header>
        <KPI.Title>{label}</KPI.Title>
      </KPI.Header>
    </KPI>
  );
}

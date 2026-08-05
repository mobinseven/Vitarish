"use client";

import { Widget } from "@heroui-pro/react";
import { KPI } from "@heroui-pro/react/kpi";
import { KPIGroup } from "@heroui-pro/react/kpi-group";
import { SeaLoadingState, SeaEmptyState } from "../state/SeaStateSurface";
import { useSeaStatistics } from "../../hooks/statistics/useSeaStatistics";
import { useLocalizedText } from "../../localization";

export function SeaDashboardPage() {
  const { t } = useLocalizedText();
  const { data: stats, isLoading } = useSeaStatistics();

  if (isLoading) {
    return <SeaLoadingState label={t("sea.stats.loading")} />;
  }

  if (!stats) {
    return <SeaEmptyState title={t("sea.stats.emptyState")} />;
  }

  return (
    <div className="flex flex-col gap-6">
      <h1 className="text-2xl font-bold">{t("sea.stats.title")}</h1>

      <Widget>
        <Widget.Header>
          <Widget.Title>{t("sea.stats.title")}</Widget.Title>
        </Widget.Header>
        <Widget.Content>
          <KPIGroup orientation="horizontal">
            <KPI>
              <KPI.Content>
                <KPI.Value
                  maximumFractionDigits={0}
                  value={Number(stats.projects) || 0}
                />
              </KPI.Content>
              <KPI.Header>
                <KPI.Title>{t("sea.stats.projects")}</KPI.Title>
              </KPI.Header>
            </KPI>
            <KPI>
              <KPI.Content>
                <KPI.Value
                  maximumFractionDigits={0}
                  value={Number(stats.procedures) || 0}
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
                  value={Number(stats.events) || 0}
                />
              </KPI.Content>
              <KPI.Header>
                <KPI.Title>{t("sea.stats.events")}</KPI.Title>
              </KPI.Header>
            </KPI>
            <KPI>
              <KPI.Content>
                <KPI.Value
                  maximumFractionDigits={0}
                  value={Number(stats.roles) || 0}
                />
              </KPI.Content>
              <KPI.Header>
                <KPI.Title>{t("sea.stats.roles")}</KPI.Title>
              </KPI.Header>
            </KPI>
            <KPI>
              <KPI.Content>
                <KPI.Value
                  maximumFractionDigits={0}
                  value={Number(stats.attachments) || 0}
                />
              </KPI.Content>
              <KPI.Header>
                <KPI.Title>{t("sea.stats.attachments")}</KPI.Title>
              </KPI.Header>
            </KPI>
            <KPI>
              <KPI.Content>
                <KPI.Value
                  maximumFractionDigits={0}
                  value={Number(stats.reminders) || 0}
                />
              </KPI.Content>
              <KPI.Header>
                <KPI.Title>{t("sea.stats.reminders")}</KPI.Title>
              </KPI.Header>
            </KPI>
          </KPIGroup>
        </Widget.Content>
      </Widget>
    </div>
  );
}

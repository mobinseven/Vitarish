"use client";
import { DetailGrid, type DetailGridItem } from "@vitarish/web-ui";

import { KPI, KPIGroup, Widget } from "@heroui-pro/react";
import { ClinicEmptyState } from "./ClinicStateSurface";
import type { PatientViewDto } from "../types";
import { useLocalizedText, formatDateTime } from "../../../localization";

interface PatientOverviewPanelProps {
  patient: PatientViewDto;
}

function formatProgressState(state: string | null | undefined, fallback: string): string {
  if (!state) return fallback;
  return state.replace(/([A-Z])/g, " $1").trim();
}

export function PatientOverviewPanel({ patient }: PatientOverviewPanelProps) {
  const { t, locale } = useLocalizedText();
  const stats = patient.stats;

  return (
    <Widget>
      <Widget.Header>
        <Widget.Title>{t("clinic.patient.overview.title")}</Widget.Title>
      </Widget.Header>
      <Widget.Content className="flex flex-col gap-4">
        {stats ? (
          <>
            <DetailGrid
              items={[
                {
                  label: t("clinic.patient.overview.progress"),
                  value: formatProgressState(stats.state, t("clinic.patient.overview.notStarted")),
                },
                {
                  label: t("clinic.patient.overview.unread"),
                  value: stats.unread ? t("clinic.patient.overview.yes") : t("clinic.patient.overview.no"),
                },
                ...(stats.lastUpdate
                  ? [{
                      label: t("clinic.patient.overview.lastUpdate"),
                      value: formatDateTime(stats.lastUpdate, locale),
                    }]
                  : []),
              ]}
            />

            <KPIGroup orientation="vertical">
              <MetricKpi label={t("clinic.patient.overview.procedures")} value={Number(stats.procedures) || 0} />
              <KPIGroup.Separator />
              <MetricKpi label={t("clinic.patient.overview.events")} value={Number(stats.events) || 0} />
              <KPIGroup.Separator />
              <MetricKpi label={t("clinic.patient.overview.committedEvents")} value={Number(stats.committedEvents) || 0} />
              <KPIGroup.Separator />
              <MetricKpi label={t("clinic.patient.overview.eventItems")} value={Number(stats.eventItems) || 0} />
              <KPIGroup.Separator />
              <MetricKpi label={t("clinic.patient.overview.newItems")} value={Number(stats.newItems) || 0} />
            </KPIGroup>
          </>
        ) : (
          <ClinicEmptyState title={t("clinic.patient.overview.noStats")} />
        )}
      </Widget.Content>
    </Widget>
  );
}

function MetricKpi({ label, value }: { label: string; value: number }) {
  return (
    <KPI>
      <KPI.Header>
        <KPI.Title>{label}</KPI.Title>
      </KPI.Header>
      <KPI.Content>
        <KPI.Value maximumFractionDigits={0} value={value} />
      </KPI.Content>
    </KPI>
  );
}
"use client";

import { Card } from "@heroui/react";
import type { PatientViewDto } from "../types";
import { useLocalizedText, formatDateTime } from "@/localization";

interface PatientOverviewPanelProps {
  patient: PatientViewDto;
}

function formatProgressState(state: string | null | undefined): string {
  if (!state) return "Not Started";
  return state.replace(/([A-Z])/g, " $1").trim();
}

export function PatientOverviewPanel({ patient }: PatientOverviewPanelProps) {
  const { t, locale } = useLocalizedText();
  const stats = patient.stats;

  return (
    <Card>
      <Card.Header>
        <h2 className="text-lg font-semibold">{t("clinic.patient.overview.title")}</h2>
      </Card.Header>
      <Card.Content className="flex flex-col gap-4">
        {stats ? (
          <>
            <div className="flex flex-col gap-1">
              <span className="text-sm text-muted">{t("clinic.patient.overview.progress")}</span>
              <span className="text-sm font-medium">
                {formatProgressState(stats.state)}
              </span>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <StatItem label={t("clinic.patient.overview.procedures")} value={Number(stats.procedures) || 0} />
              <StatItem label={t("clinic.patient.overview.events")} value={Number(stats.events) || 0} />
              <StatItem
                label={t("clinic.patient.overview.committedEvents")}
                value={Number(stats.committedEvents) || 0}
              />
              <StatItem label={t("clinic.patient.overview.eventItems")} value={Number(stats.eventItems) || 0} />
              <StatItem label={t("clinic.patient.overview.newItems")} value={Number(stats.newItems) || 0} />
            </div>

            <div className="flex items-center gap-2">
              <span className="text-sm text-muted">{t("clinic.patient.overview.unread")}:</span>
              <span
                className={`text-sm font-medium ${stats.unread ? "text-warning" : "text-muted"}`}
              >
                {stats.unread ? t("clinic.patient.overview.yes") : t("clinic.patient.overview.no")}
              </span>
            </div>

            {stats.lastUpdate && (
              <div className="flex flex-col gap-1">
                <span className="text-sm text-muted">{t("clinic.patient.overview.lastUpdate")}</span>
                <span className="text-sm">
                  {formatDateTime(stats.lastUpdate, locale)}
                </span>
              </div>
            )}
          </>
        ) : (
          <p className="text-sm text-muted">
            {t("clinic.patient.overview.noStats")}
          </p>
        )}
      </Card.Content>
    </Card>
  );
}

function StatItem({
  label,
  value,
}: {
  label: string;
  value: number;
}) {
  return (
    <div className="flex flex-col gap-1">
      <span className="text-sm text-muted">{label}</span>
      <span className="text-lg font-semibold">{value}</span>
    </div>
  );
}

"use client";

import { Card } from "@heroui/react";
import type { PatientViewDto } from "../types";

interface PatientOverviewPanelProps {
  patient: PatientViewDto;
}

function formatProgressState(state: string | null | undefined): string {
  if (!state) return "Not Started";
  return state.replace(/([A-Z])/g, " $1").trim();
}

export function PatientOverviewPanel({ patient }: PatientOverviewPanelProps) {
  const stats = patient.stats;

  return (
    <Card className="glass-card">
      <Card.Header>
        <h2 className="text-lg font-semibold">Overview</h2>
      </Card.Header>
      <Card.Content className="flex flex-col gap-4">
        {stats ? (
          <>
            <div className="flex flex-col gap-1">
              <span className="text-sm text-default-500">Progress</span>
              <span className="text-sm font-medium">
                {formatProgressState(stats.state)}
              </span>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <StatItem label="Procedures" value={Number(stats.procedures) || 0} />
              <StatItem label="Events" value={Number(stats.events) || 0} />
              <StatItem
                label="Committed Events"
                value={Number(stats.committedEvents) || 0}
              />
              <StatItem label="Event Items" value={Number(stats.eventItems) || 0} />
              <StatItem label="New Items" value={Number(stats.newItems) || 0} />
            </div>

            <div className="flex items-center gap-2">
              <span className="text-sm text-default-500">Unread:</span>
              <span
                className={`text-sm font-medium ${stats.unread ? "text-warning" : "text-default-500"}`}
              >
                {stats.unread ? "Yes" : "No"}
              </span>
            </div>

            {stats.lastUpdate && (
              <div className="flex flex-col gap-1">
                <span className="text-sm text-default-500">Last Update</span>
                <span className="text-sm">
                  {new Date(stats.lastUpdate).toLocaleString()}
                </span>
              </div>
            )}
          </>
        ) : (
          <p className="text-sm text-default-500">
            No statistics available for this patient.
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
      <span className="text-sm text-default-500">{label}</span>
      <span className="text-lg font-semibold">{value}</span>
    </div>
  );
}

"use client";

import { Card } from "@heroui/react";

export function ClinicStatisticsDashboard() {
  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold">Clinic Statistics</h1>
        <p className="text-sm text-muted">View clinic performance metrics and analytics</p>
      </div>

      <Card className="glass-card">
        <Card.Content className="flex flex-col items-center justify-center py-12">
          <p className="text-sm text-muted">
            Clinic performance metrics and analytics will be available here soon.
          </p>
        </Card.Content>
      </Card>
    </div>
  );
}

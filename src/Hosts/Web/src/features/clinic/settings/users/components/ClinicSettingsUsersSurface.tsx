"use client";

import { Card } from "@heroui/react";

export function ClinicSettingsUsersSurface() {
  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold">Clinic Users</h1>
        <p className="text-sm text-muted">Manage clinic staff accounts and permissions</p>
      </div>

      <Card className="glass-card">
        <Card.Content className="flex flex-col items-center justify-center py-12">
          <p className="text-sm text-muted">User management coming soon.</p>
        </Card.Content>
      </Card>
    </div>
  );
}

"use client";

import { Card } from "@heroui/react";

export function ServiceProvidersPlaceholder() {
  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold">Service Providers</h1>
        <p className="text-sm text-muted">Manage healthcare providers and their specialties</p>
      </div>

      <Card className="glass-card">
        <Card.Content className="flex flex-col items-center justify-center py-12">
          <p className="text-sm text-muted">
            Service providers management coming soon.
          </p>
        </Card.Content>
      </Card>
    </div>
  );
}

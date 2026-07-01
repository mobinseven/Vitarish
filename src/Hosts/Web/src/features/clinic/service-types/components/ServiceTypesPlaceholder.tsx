"use client";

import { Card } from "@heroui/react";

export function ServiceTypesPlaceholder() {
  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold">Service Types</h1>
        <p className="text-sm text-muted">Manage service types offered by the clinic</p>
      </div>

      <Card className="glass-card">
        <Card.Content className="flex flex-col items-center justify-center py-12">
          <p className="text-sm text-muted">
            Service types management coming soon.
          </p>
        </Card.Content>
      </Card>
    </div>
  );
}

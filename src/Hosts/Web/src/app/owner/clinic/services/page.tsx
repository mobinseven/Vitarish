import type { Metadata } from "next";
import { Card } from "@heroui/react";

export const metadata: Metadata = {
  title: "Services | Vitarish",
  description: "Manage individual services and their configurations",
};

export default function ClinicServicesPage() {
  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold">Services</h1>
        <p className="text-sm text-muted">Manage individual services and their configurations</p>
      </div>

      <Card className="glass-card">
        <Card.Content className="flex flex-col items-center justify-center py-12">
          <p className="text-sm text-muted">
            Services management coming soon.
          </p>
        </Card.Content>
      </Card>
    </div>
  );
}

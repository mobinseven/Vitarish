"use client";

import { useRouter } from "next/navigation";
import { Card, Button } from "@heroui/react";
import { Routes } from "@/app/routes";

export default function NewPatientRoute() {
  const router = useRouter();

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Button
          variant="ghost"
          className="mb-2"
          onPress={() => router.push(Routes.Clinic.Patients.Index)}
        >
          ← Back to Patients
        </Button>
        <h1 className="text-2xl font-bold">New Patient</h1>
      </div>

      <Card className="glass-card">
        <Card.Header>
          <h2 className="text-lg font-semibold">Create Patient</h2>
        </Card.Header>
        <Card.Content className="flex flex-col gap-4">
          <p className="text-default-500">
            Patient creation is not yet available. This feature requires a
            backend endpoint that has not been implemented yet.
          </p>
        </Card.Content>
      </Card>
    </div>
  );
}

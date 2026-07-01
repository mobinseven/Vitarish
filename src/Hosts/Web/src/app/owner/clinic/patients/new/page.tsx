"use client";

import { useRouter } from "next/navigation";
import { Card, Button } from "@heroui/react";
import { Routes } from "@/app/routes";
import { useLocalizedText } from "@/localization";

export default function NewPatientRoute() {
  const router = useRouter();
  const { t } = useLocalizedText();

  return (
    <div className="flex flex-col gap-6">
      <div>
        <Button
          variant="ghost"
          className="mb-2"
          onPress={() => router.push(Routes.Clinic.Patients.Index)}
        >
          ← {t("clinic.patients.backToPatients")}
        </Button>
        <h1 className="text-2xl font-bold">{t("clinic.patients.newPatient")}</h1>
      </div>

      <Card className="glass-card">
        <Card.Header>
          <h2 className="text-lg font-semibold">{t("clinic.patients.createPatient")}</h2>
        </Card.Header>
        <Card.Content className="flex flex-col gap-4">
          <p className="text-default-500">
            {t("clinic.patients.createUnavailable")}
          </p>
        </Card.Content>
      </Card>
    </div>
  );
}

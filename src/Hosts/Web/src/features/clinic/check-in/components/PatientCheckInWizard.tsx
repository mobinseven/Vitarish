"use client";

import { useState } from "react";
import { Card } from "@heroui/react";
import { IdentificationLookup } from "@/features/clinic/components/IdentificationLookup";
import { useLocalizedText } from "@/localization";

type CheckInStep = "find-patient" | "select-shift" | "confirm";

interface PatientCheckInWizardProps {
  onPatientSelected: (patientId: string) => void;
}

export function PatientCheckInWizard({
  onPatientSelected,
}: PatientCheckInWizardProps) {
  const [step, setStep] = useState<CheckInStep>("find-patient");
  const [selectedPatientId, setSelectedPatientId] = useState<string | null>(null);
  const { t } = useLocalizedText();

  const handlePatientSelect = (patientId: string) => {
    setSelectedPatientId(patientId);
    onPatientSelected(patientId);
    setStep("select-shift");
  };

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold">{t("clinic.checkIn.title")}</h1>
        <p className="text-sm text-muted">{t("clinic.checkIn.description")}</p>
      </div>

      <Card className="glass-card">
        <Card.Header>
          <h2 className="text-lg font-semibold">
            {t("clinic.checkIn.step1")}
          </h2>
        </Card.Header>
        <Card.Content>
          <IdentificationLookup onSelectPatient={handlePatientSelect} />
        </Card.Content>
      </Card>

      {step === "select-shift" && selectedPatientId && (
        <Card className="glass-card">
          <Card.Header>
            <h2 className="text-lg font-semibold">
              {t("clinic.checkIn.step2")}
            </h2>
          </Card.Header>
          <Card.Content>
            <p className="text-sm text-muted">
              {t("clinic.checkIn.shiftUnavailable")}
            </p>
          </Card.Content>
        </Card>
      )}

      {step === "confirm" && (
        <Card className="glass-card">
          <Card.Header>
            <h2 className="text-lg font-semibold">
              {t("clinic.checkIn.step3")}
            </h2>
          </Card.Header>
          <Card.Content>
            <p className="text-sm text-muted">
              {t("clinic.checkIn.confirmationComingSoon")}
            </p>
          </Card.Content>
        </Card>
      )}
    </div>
  );
}

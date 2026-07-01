"use client";

import { useState } from "react";
import { Card, Button } from "@heroui/react";
import { IdentificationLookup } from "@/features/clinic/components/IdentificationLookup";

type CheckInStep = "find-patient" | "select-shift" | "confirm";

interface PatientCheckInWizardProps {
  onPatientSelected: (patientId: string) => void;
}

export function PatientCheckInWizard({
  onPatientSelected,
}: PatientCheckInWizardProps) {
  const [step, setStep] = useState<CheckInStep>("find-patient");
  const [selectedPatientId, setSelectedPatientId] = useState<string | null>(null);

  const handlePatientSelect = (patientId: string) => {
    setSelectedPatientId(patientId);
    onPatientSelected(patientId);
    setStep("select-shift");
  };

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold">Patient Check-in</h1>
        <p className="text-sm text-muted">Register a patient for their appointment</p>
      </div>

      <Card className="glass-card">
        <Card.Header>
          <h2 className="text-lg font-semibold">
            Step 1: Find Patient
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
              Step 2: Select Shift
            </h2>
          </Card.Header>
          <Card.Content>
            <p className="text-sm text-muted">
              Shift selection coming soon. Patient ID: {selectedPatientId}
            </p>
          </Card.Content>
        </Card>
      )}

      {step === "confirm" && (
        <Card className="glass-card">
          <Card.Header>
            <h2 className="text-lg font-semibold">
              Step 3: Confirm Check-in
            </h2>
          </Card.Header>
          <Card.Content>
            <p className="text-sm text-muted">
              Check-in confirmation coming soon.
            </p>
          </Card.Content>
        </Card>
      )}
    </div>
  );
}

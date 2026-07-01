"use client";

import { useCallback, useState } from "react";
import { Card, Button } from "@heroui/react";
import { PatientFindOrCreate } from "@/features/clinic/patients/find-or-create/components/PatientFindOrCreate";
import { useLocalizedText } from "@/localization";
import type { ShiftReservationReadDto } from "@/api/generated/appointments/model";

type CheckInStep = "find-patient" | "select-shift" | "confirm";

interface PatientCheckInWizardProps {
  onPatientSelected: (patientId: string) => void;
  onCheckInComplete?: (patientId: string, shiftReservation: ShiftReservationReadDto) => void;
}

export function PatientCheckInWizard({
  onPatientSelected,
  onCheckInComplete,
}: PatientCheckInWizardProps) {
  const { t } = useLocalizedText();
  const [step, setStep] = useState<CheckInStep>("find-patient");
  const [selectedPatientId, setSelectedPatientId] = useState<string | null>(null);
  const [selectedShift, setSelectedShift] = useState<ShiftReservationReadDto | null>(null);

  const handlePatientSelect = useCallback(
    (patientId: string) => {
      setSelectedPatientId(patientId);
      onPatientSelected(patientId);
      setStep("select-shift");
    },
    [onPatientSelected]
  );

  const handleConfirm = useCallback(() => {
    if (selectedPatientId && selectedShift) {
      onCheckInComplete?.(selectedPatientId, selectedShift);
    }
  }, [selectedPatientId, selectedShift, onCheckInComplete]);

  const handleBackToFind = useCallback(() => {
    setStep("find-patient");
    setSelectedPatientId(null);
    setSelectedShift(null);
  }, []);

  return (
    <div className="flex flex-col gap-6">
      <div>
        <h1 className="text-2xl font-bold">{t("clinic.checkIn.title")}</h1>
        <p className="text-sm text-default-500">{t("clinic.checkIn.description")}</p>
      </div>

      {/* Step indicators */}
      <div className="flex items-center gap-2 text-sm">
        <StepIndicator step={1} label={t("clinic.checkIn.step1")} isActive={step === "find-patient"} isComplete={step !== "find-patient"} />
        <div className="h-px w-8 bg-default-200" />
        <StepIndicator step={2} label={t("clinic.checkIn.step2")} isActive={step === "select-shift"} isComplete={step === "confirm"} />
        <div className="h-px w-8 bg-default-200" />
        <StepIndicator step={3} label={t("clinic.checkIn.step3")} isActive={step === "confirm"} isComplete={false} />
      </div>

      {step === "find-patient" && (
        <PatientFindOrCreate onPatientSelected={handlePatientSelect} />
      )}

      {step === "select-shift" && selectedPatientId && (
        <Card>
          <Card.Header>
            <div className="flex items-center justify-between">
              <h2 className="text-lg font-semibold">{t("clinic.checkIn.step2")}</h2>
              <Button variant="ghost" onPress={handleBackToFind}>
                ← {t("clinic.checkIn.changePatient")}
              </Button>
            </div>
          </Card.Header>
          <Card.Content>
            <p className="text-sm text-default-500 mb-4">
              {t("clinic.checkIn.selectShiftDescription")}
            </p>
            <ShiftSelectionPanel />
          </Card.Content>
        </Card>
      )}

      {step === "confirm" && selectedPatientId && selectedShift && (
        <Card>
          <Card.Header>
            <h2 className="text-lg font-semibold">{t("clinic.checkIn.step3")}</h2>
          </Card.Header>
          <Card.Content className="flex flex-col gap-4">
            <div className="grid gap-3 sm:grid-cols-2">
              <div>
                <span className="text-xs text-default-500">{t("clinic.checkIn.shiftDate")}</span>
                <p className="text-sm">{selectedShift.date}</p>
              </div>
              <div>
                <span className="text-xs text-default-500">{t("clinic.checkIn.shiftTime")}</span>
                <p className="text-sm">{selectedShift.startTime} - {selectedShift.endTime}</p>
              </div>
              <div>
                <span className="text-xs text-default-500">{t("clinic.checkIn.availableSeats")}</span>
                <p className="text-sm">{selectedShift.availableSeats}</p>
              </div>
            </div>

            <div className="flex justify-end gap-2">
              <Button variant="ghost" onPress={() => setStep("select-shift")}>
                {t("clinic.checkIn.changeShift")}
              </Button>
              <Button variant="primary" onPress={handleConfirm}>
                {t("clinic.checkIn.confirmCheckIn")}
              </Button>
            </div>
          </Card.Content>
        </Card>
      )}
    </div>
  );
}

function StepIndicator({
  step,
  label,
  isActive,
  isComplete,
}: {
  step: number;
  label: string;
  isActive: boolean;
  isComplete: boolean;
}) {
  return (
    <div className={`flex items-center gap-2 ${isActive ? "text-accent font-medium" : isComplete ? "text-success" : "text-default-500"}`}>
      <span className={`flex h-6 w-6 items-center justify-center rounded-full text-xs ${
        isActive ? "bg-accent text-white" : isComplete ? "bg-success text-white" : "bg-default-200 text-default-600"
      }`}>
        {isComplete ? "✓" : step}
      </span>
      <span className="text-sm">{label}</span>
    </div>
  );
}

function ShiftSelectionPanel() {
  const { t } = useLocalizedText();

  return (
    <div className="flex flex-col items-center gap-4 py-8">
      <p className="text-sm text-default-500">{t("clinic.checkIn.shiftUnavailable")}</p>
    </div>
  );
}

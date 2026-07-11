"use client";
import { DetailGrid, type DetailGridItem } from "@vitarish/web-ui";

import { useCallback, useState } from "react";
import { Button } from "@heroui/react";
import { Stepper, Widget } from "@heroui-pro/react";
import type { ShiftReservationReadDto } from "../../../../api/generated/appointments/model";
import { PatientFindOrCreate } from "../../../../features/clinic/patients/find-or-create/components/PatientFindOrCreate";
import { ClinicEmptyState } from "../../../../features/clinic/components/ClinicStateSurface";
import { useLocalizedText } from "../../../../localization";

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
  const currentStep = step === "find-patient" ? 0 : step === "select-shift" ? 1 : 2;
  const steps = [
    t("clinic.checkIn.step1"),
    t("clinic.checkIn.step2"),
    t("clinic.checkIn.step3"),
  ];

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
        <p className="text-sm text-muted">{t("clinic.checkIn.description")}</p>
      </div>

      <Stepper currentStep={currentStep}>
        {steps.map((stepLabel) => (
          <Stepper.Step key={stepLabel}>
            <Stepper.Indicator />
            <Stepper.Content>
              <Stepper.Title>{stepLabel}</Stepper.Title>
            </Stepper.Content>
            <Stepper.Separator />
          </Stepper.Step>
        ))}
      </Stepper>

      {step === "find-patient" && (
        <PatientFindOrCreate onPatientSelected={handlePatientSelect} />
      )}

      {step === "select-shift" && selectedPatientId && (
        <Widget>
          <Widget.Header>
            <div className="flex items-center justify-between">
              <Widget.Title>{t("clinic.checkIn.step2")}</Widget.Title>
              <Button variant="ghost" onPress={handleBackToFind}>
                <i aria-hidden className="fa-solid fa-arrow-left" />
                {t("clinic.checkIn.changePatient")}
              </Button>
            </div>
            <Widget.Description>{t("clinic.checkIn.selectShiftDescription")}</Widget.Description>
          </Widget.Header>
          <Widget.Content>
            <ShiftSelectionPanel />
          </Widget.Content>
        </Widget>
      )}

      {step === "confirm" && selectedPatientId && selectedShift && (
        <Widget>
          <Widget.Header>
            <Widget.Title>{t("clinic.checkIn.step3")}</Widget.Title>
          </Widget.Header>
          <Widget.Content className="flex flex-col gap-4">
            <DetailGrid
              items={[
                { label: t("clinic.checkIn.shiftDate"), value: selectedShift.date },
                { label: t("clinic.checkIn.shiftTime"), value: `${selectedShift.startTime} - ${selectedShift.endTime}` },
                { label: t("clinic.checkIn.availableSeats"), value: selectedShift.availableSeats },
              ]}
            />

            <div className="flex justify-end gap-2">
              <Button variant="ghost" onPress={() => setStep("select-shift")}>
                {t("clinic.checkIn.changeShift")}
              </Button>
              <Button variant="primary" onPress={handleConfirm}>
                {t("clinic.checkIn.confirmCheckIn")}
              </Button>
            </div>
          </Widget.Content>
        </Widget>
      )}
    </div>
  );
}

function ShiftSelectionPanel() {
  const { t } = useLocalizedText();

  return (
    <ClinicEmptyState title={t("clinic.checkIn.shiftUnavailable")} />
  );
}
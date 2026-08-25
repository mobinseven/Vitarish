"use client";

import { Button, Chip } from "@heroui/react";
import { ItemCard, PressableFeedback } from "@heroui-pro/react";
import { useCallback, useState } from "react";

import { FaIcon } from "@vitarish/ui";
import { useLocalizedText } from "../../localization/index";
import { PatientFindOrCreate } from "../patients/find-or-create/PatientFindOrCreate";
import type { ShiftReservationReadDto } from "../../api/generated/appointments/model";

type WizardStep = "find-patient" | "select-shift" | "success";

interface PatientCheckInWizardProps {
  onPatientSelected: (patientId: string) => void;
  onCheckInComplete?: (
    patientId: string,
    shiftReservation: ShiftReservationReadDto,
  ) => void;
}

const MOCK_TODAY_SHIFTS: ShiftReservationReadDto[] = [
  {
    shiftId: "1",
    startTime: "08:00",
    endTime: "10:00",
    availableSeats: 5,
    totalSeats: 8,
  },
  {
    shiftId: "2",
    startTime: "10:00",
    endTime: "12:00",
    availableSeats: 3,
    totalSeats: 8,
  },
  {
    shiftId: "3",
    startTime: "14:00",
    endTime: "16:00",
    availableSeats: 7,
    totalSeats: 10,
  },
];

export function PatientCheckInWizard({
  onPatientSelected,
  onCheckInComplete,
}: PatientCheckInWizardProps) {
  const { t } = useLocalizedText();
  const [step, setStep] = useState<WizardStep>("find-patient");
  const [selectedPatientId, setSelectedPatientId] = useState<string | null>(
    null,
  );
  const [patientName, setPatientName] = useState("");
  const [completedShiftId, setCompletedShiftId] = useState<string | null>(null);

  const handlePatientSelected = useCallback(
    (patientId: string) => {
      setSelectedPatientId(patientId);
      // Derive name from hook isn't available here — pass a placeholder
      // The parent should set the real name via onPatientSelected
      setPatientName("");
      setStep("select-shift");
      onPatientSelected(patientId);
    },
    [onPatientSelected],
  );

  const handleShiftSelect = useCallback(
    (shift: ShiftReservationReadDto) => {
      if (!selectedPatientId) return;
      setCompletedShiftId(shift.shiftId ?? null);
      setStep("success");
      onCheckInComplete?.(selectedPatientId, shift);
      setTimeout(() => {
        setStep("find-patient");
        setSelectedPatientId(null);
        setPatientName("");
        setCompletedShiftId(null);
      }, 1800);
    },
    [selectedPatientId, onCheckInComplete],
  );

  const handleStartOver = useCallback(() => {
    setStep("find-patient");
    setSelectedPatientId(null);
    setPatientName("");
    setCompletedShiftId(null);
  }, []);

  return (
    <div className="mx-auto flex max-w-xl flex-col gap-4">
      {/* ─── Beat 1: Who ─── */}
      <PatientFindOrCreate
        onPatientSelected={handlePatientSelected}
        hideSelectedState={step !== "find-patient"}
      />

      {/* ─── Beat 2: Which shift ─── */}
      {step === "select-shift" && selectedPatientId && (
        <div className="flex flex-col gap-3">
          <div className="flex items-center justify-between">
            <Chip size="sm" variant="soft" color="accent">
              <FaIcon icon="user" aria-hidden="true" className="mr-1" />
              {patientName || t("clinic.checkIn.patientSelected")}
            </Chip>
            <Button
              isIconOnly
              variant="ghost"
              size="sm"
              onPress={handleStartOver}
              aria-label={t("clinic.checkIn.changePatient")}
            >
              <FaIcon icon="xmark" aria-hidden="true" />
            </Button>
          </div>

          <div className="flex flex-col gap-1.5">
            {MOCK_TODAY_SHIFTS.map((shift) => (
              <ShiftCard
                key={shift.shiftId}
                shift={shift}
                onSelect={handleShiftSelect}
                isCompleted={completedShiftId === shift.shiftId}
              />
            ))}
            {MOCK_TODAY_SHIFTS.length === 0 && (
              <p className="py-3 text-center text-sm text-muted">
                {t("clinic.checkIn.noShiftsToday")}
              </p>
            )}
          </div>
        </div>
      )}

      {/* ─── Success ─── */}
      {step === "success" && (
        <div className="rounded-medium border border-success/30 bg-success/5 p-4 text-center">
          <FaIcon
            icon="circle-check"
            aria-hidden="true"
            className="mb-2 text-2xl text-success"
          />
          <p className="text-sm font-medium text-success">
            {t("clinic.checkIn.confirmed")}
          </p>
        </div>
      )}
    </div>
  );
}

/* ─── Shift Card ─── */

interface ShiftCardProps {
  shift: ShiftReservationReadDto;
  onSelect: (shift: ShiftReservationReadDto) => void;
  isCompleted: boolean;
}

function ShiftCard({ shift, onSelect, isCompleted }: ShiftCardProps) {
  const { t } = useLocalizedText();
  const seats =
    typeof shift.availableSeats === "number" ? shift.availableSeats : null;
  const isFull = seats !== null && seats <= 0;

  return (
    <ItemCard<"button">
      className="relative w-full cursor-pointer overflow-hidden text-start"
      render={(props) => (
        <button
          type="button"
          {...props}
          onClick={(event) => {
            props.onClick?.(event);
            if (!isFull) onSelect(shift);
          }}
          disabled={isFull}
        />
      )}
      variant="outline"
    >
      <PressableFeedback.Highlight />
      <ItemCard.Icon>
        <FaIcon
          icon={isCompleted ? "circle-check" : "clock"}
          aria-hidden="true"
          className={isCompleted ? "text-success" : "text-muted"}
        />
      </ItemCard.Icon>
      <ItemCard.Content>
        <ItemCard.Title>
          {shift.startTime} – {shift.endTime}
        </ItemCard.Title>
        {seats !== null && (
          <ItemCard.Description>
            {isFull
              ? t("clinic.checkIn.shiftFull")
              : t("clinic.checkIn.seatsAvailable", { count: String(seats) })}
          </ItemCard.Description>
        )}
      </ItemCard.Content>
      <ItemCard.Action>
        {isCompleted ? (
          <FaIcon icon="check" aria-hidden="true" className="text-success" />
        ) : (
          <FaIcon
            icon="chevron-right"
            aria-hidden="true"
            className="text-muted"
          />
        )}
      </ItemCard.Action>
    </ItemCard>
  );
}

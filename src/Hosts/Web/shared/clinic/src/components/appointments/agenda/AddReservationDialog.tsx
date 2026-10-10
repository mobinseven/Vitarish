"use client";

import { useCallback, useEffect, useMemo, useState } from "react";
import { Button, Chip, Modal } from "@heroui/react";

import { FaIcon, contextColorClassName } from "@vitarish/ui";
import { formatDate, useLocalizedText } from "../../../localization/index";
import {
  useCreateReservation,
  useUpdateReservationStatus,
} from "../../../hooks/appointments/reservations/useOwnerReservations";
import { PatientFindOrCreate } from "../../patients/find-or-create/PatientFindOrCreate";
import type { ShiftReadDto } from "../../../api/appointments/appointmentsApi";

interface AddReservationDialogProps {
  isOpen: boolean;
  onClose: () => void;
  shift: ShiftReadDto;
  date: string;
  availableSeats: number;
}

export function AddReservationDialog({
  isOpen,
  onClose,
  shift,
  date,
  availableSeats,
}: AddReservationDialogProps) {
  const { t, locale } = useLocalizedText();
  const [selectedPatientId, setSelectedPatientId] = useState<string | null>(
    null,
  );
  const [success, setSuccess] = useState(false);
  const [bookingError, setBookingError] = useState(false);

  const createReservation = useCreateReservation();
  const confirmReservation = useUpdateReservationStatus();

  const isFull = availableSeats <= 0;
  const localizedDate = formatDate(date, locale);
  const formatShiftTime = (time: string) =>
    formatDate(`${date}T${time.length === 5 ? `${time}:00` : time}`, locale, {
      hour: "numeric",
      minute: "2-digit",
    });

  const handleClose = useCallback(() => {
    setSelectedPatientId(null);
    setSuccess(false);
    setBookingError(false);
    onClose();
  }, [onClose]);

  useEffect(() => {
    if (!success) return;
    const timer = window.setTimeout(handleClose, 1200);
    return () => window.clearTimeout(timer);
  }, [success, handleClose]);

  const state = useMemo(
    () => ({
      isOpen,
      setOpen: (open: boolean) => {
        if (!open) handleClose();
      },
      open: () => undefined,
      close: handleClose,
      toggle: () => {
        if (isOpen) handleClose();
      },
    }),
    // eslint-disable-next-line react-hooks/exhaustive-deps
    [isOpen, handleClose],
  );

  const handlePatientSelected = useCallback((patientId: string) => {
    setSelectedPatientId(patientId);
  }, []);

  const handleBook = useCallback(async () => {
    if (!selectedPatientId || isFull) return;
    setBookingError(false);
    try {
      const result = await createReservation.mutateAsync({
        shiftId: shift.id,
        date,
        userId: selectedPatientId,
      });
      if (!result.succeeded || !result.entityId) {
        setBookingError(true);
        return;
      }
      // Auto-confirm: set status to 1 (confirmed) immediately after creation
      const confirmation = await confirmReservation.mutateAsync({
        id: result.entityId,
        status: 1,
      });
      if (!confirmation.succeeded) {
        setBookingError(true);
        return;
      }
      setSuccess(true);
    } catch {
      setBookingError(true);
    }
  }, [
    selectedPatientId,
    isFull,
    createReservation,
    confirmReservation,
    shift.id,
    date,
    handleClose,
  ]);

  if (!isOpen) return null;

  return (
    <Modal state={state}>
      <Modal.Backdrop isDismissable>
        <Modal.Container placement="center">
          <Modal.Dialog>
            <Modal.Header>
              <Modal.Heading>
                <div className="flex items-center gap-2">
                  <FaIcon
                    icon="calendar-plus"
                    aria-hidden="true"
                    className="text-muted"
                  />
                  <span>{t("clinic.appointments.agenda.addReservation")}</span>
                </div>
              </Modal.Heading>
              <p className="text-sm text-muted">
                {localizedDate} · {formatShiftTime(shift.startTime)} –{" "}
                {formatShiftTime(shift.endTime)}
              </p>
            </Modal.Header>
            <Modal.Body>
              {success ? (
                <div className="flex flex-col items-center gap-2 py-6">
                  <FaIcon
                    icon="circle-check"
                    aria-hidden="true"
                    className="text-3xl text-success"
                  />
                  <p className="text-sm font-medium text-success">
                    {t("clinic.appointments.agenda.reservationCreated")}
                  </p>
                </div>
              ) : (
                <div className="flex flex-col gap-4">
                  <div className="flex items-center justify-between">
                    <span className="text-sm text-muted">
                      {t("clinic.appointments.agenda.seatsAvailable", {
                        available: String(availableSeats),
                        total: String(shift.totalSeats),
                      })}
                    </span>
                    {isFull && (
                      <Chip size="sm" variant="soft" color="danger">
                        {t("clinic.checkIn.shiftFull")}
                      </Chip>
                    )}
                  </div>

                  {selectedPatientId ? (
                    <div className="flex items-center gap-2">
                      <Chip size="sm" variant="soft" className={`${contextColorClassName("patient-record")} bg-tint-soft text-tint-soft-foreground`}>
                        <FaIcon
                          icon="user"
                          aria-hidden="true"
                          className="mr-1"
                        />
                        {t("clinic.checkIn.patientSelected")}
                      </Chip>
                      <Button
                        isIconOnly
                        variant="ghost"
                        size="sm"
                        onPress={() => setSelectedPatientId(null)}
                        aria-label={t("clinic.checkIn.changePatient")}
                      >
                        <FaIcon icon="xmark" aria-hidden="true" />
                      </Button>
                    </div>
                  ) : (
                    <PatientFindOrCreate
                      onPatientSelected={handlePatientSelected}
                      hideSelectedState
                    />
                  )}
                  {bookingError && (
                    <p role="alert" className="text-sm text-danger">
                      {t("clinic.appointments.agenda.bookingFailed")}
                    </p>
                  )}
                </div>
              )}
            </Modal.Body>
            <Modal.Footer>
              <Button variant="secondary" onPress={handleClose}>
                {t("common.actions.cancel")}
              </Button>
              <Button
                variant="primary"
                onPress={handleBook}
                isDisabled={
                  !selectedPatientId ||
                  isFull ||
                  createReservation.isPending ||
                  confirmReservation.isPending
                }
                isPending={
                  createReservation.isPending || confirmReservation.isPending
                }
              >
                <FaIcon
                  icon="calendar-check"
                  aria-hidden="true"
                  className="mr-1.5"
                />
                {t("clinic.appointments.agenda.book")}
              </Button>
            </Modal.Footer>
          </Modal.Dialog>
        </Modal.Container>
      </Modal.Backdrop>
    </Modal>
  );
}

"use client";

import { useCallback, useMemo, useState } from "react";
import { Button, Chip, Modal } from "@heroui/react";

import {
  FaIcon,
  ErrorStateSurface as ClinicErrorState,
  LoadingStateSurface as ClinicLoadingState,
} from "@vitarish/web-ui";
import { useLocalizedText, type MessageKey } from "../../../localization/index";
import {
  useOwnerReservationDetail,
  useUpdateReservationStatus,
  useDeleteReservation,
} from "../../../hooks/appointments/reservations/useOwnerReservations";
import { ReservationDeleteDialog } from "../reservations/ReservationDeleteDialog";

const statusKeys: Record<number, MessageKey> = {
  0: "clinic.reservationStatus.pending",
  1: "clinic.reservationStatus.confirmed",
  2: "clinic.reservationStatus.cancelled",
  3: "clinic.reservationStatus.rejected",
  4: "clinic.reservationStatus.expired",
  5: "clinic.reservationStatus.waitlisted",
  6: "clinic.reservationStatus.present",
  7: "clinic.reservationStatus.completed",
};

const statusColors: Record<number, "warning" | "success" | "danger" | "default" | "accent"> = {
  0: "warning",
  1: "success",
  2: "danger",
  3: "danger",
  4: "default",
  5: "accent",
  6: "success",
  7: "success",
};

interface ReservationDetailDialogProps {
  isOpen: boolean;
  onClose: () => void;
  reservationId: string;
  onViewPatient?: (patientId: string) => void;
}

export function ReservationDetailDialog({
  isOpen,
  onClose,
  reservationId,
  onViewPatient,
}: ReservationDetailDialogProps) {
  const { t } = useLocalizedText();
  const [deleting, setDeleting] = useState(false);

  const query = useOwnerReservationDetail(reservationId);
  const updateStatus = useUpdateReservationStatus();
  const deletion = useDeleteReservation();

  const reservation = query.data;

  const handleClose = useCallback(() => {
    setDeleting(false);
    onClose();
  }, [onClose]);

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

  const handleConfirm = useCallback(() => {
    if (!reservation) return;
    updateStatus.mutate(
      { id: reservation.id, status: 1 },
      { onSuccess: handleClose },
    );
  }, [reservation, updateStatus, handleClose]);

  const handleDelete = useCallback(() => {
    if (!reservation) return;
    deletion.mutate(reservation.id, {
      onSuccess: handleClose,
    });
  }, [reservation, deletion, handleClose]);

  const handleViewPatient = useCallback(() => {
    if (reservation?.reservingPartyId && onViewPatient) {
      onViewPatient(reservation.reservingPartyId);
      handleClose();
    }
  }, [reservation, onViewPatient, handleClose]);

  if (!isOpen) return null;

  const isPending = reservation?.status === 0;
  const canConfirm = isPending;

  return (
    <>
      <Modal state={state}>
        <Modal.Backdrop isDismissable={!updateStatus.isPending && !deletion.isPending}>
          <Modal.Container placement="center">
            <Modal.Dialog>
              <Modal.Header>
                <Modal.Heading>
                  {t("clinic.appointments.reservations.detailTitle")}
                </Modal.Heading>
              </Modal.Header>
              <Modal.Body>
                {query.isLoading && (
                  <ClinicLoadingState label={t("common.states.loading")} />
                )}

                {query.error && (
                  <ClinicErrorState
                    title={t("clinic.appointments.reservations.notFound")}
                  />
                )}

                {reservation && (
                  <div className="flex flex-col gap-4">
                    {/* Status chip */}
                    <div className="flex items-center gap-2">
                      <Chip
                        size="sm"
                        variant="soft"
                        color={statusColors[reservation.status] ?? "default"}
                      >
                        {t(statusKeys[reservation.status] ?? "common.values.unknown")}
                      </Chip>
                      {reservation.isHold && (
                        <Chip size="sm" variant="soft" color="accent">
                          {t("clinic.fields.hold")}
                        </Chip>
                      )}
                    </div>

                    {/* Detail list */}
                    <dl className="flex flex-col gap-2 text-sm">
                      <div className="flex justify-between">
                        <dt className="text-muted">{t("clinic.fields.date")}</dt>
                        <dd className="font-medium">{reservation.date}</dd>
                      </div>
                      {reservation.estimatedSessionLength && (
                        <div className="flex justify-between">
                          <dt className="text-muted">{t("clinic.fields.sessionLength")}</dt>
                          <dd className="font-medium">{reservation.estimatedSessionLength}</dd>
                        </div>
                      )}
                      {reservation.confirmedAt && (
                        <div className="flex justify-between">
                          <dt className="text-muted">{t("clinic.fields.confirmedAt")}</dt>
                          <dd className="font-medium">{reservation.confirmedAt}</dd>
                        </div>
                      )}
                      {reservation.cancelledAt && (
                        <div className="flex justify-between">
                          <dt className="text-muted">{t("clinic.fields.cancelledAt")}</dt>
                          <dd className="font-medium">{reservation.cancelledAt}</dd>
                        </div>
                      )}
                    </dl>

                    {/* Error state */}
                    {updateStatus.isError && (
                      <p className="text-sm text-danger">
                        {t("clinic.appointments.reservations.updateFailed")}
                      </p>
                    )}
                  </div>
                )}
              </Modal.Body>
              <Modal.Footer>
                <div className="flex w-full flex-wrap items-center justify-end gap-2">
                  {reservation?.reservingPartyId && (
                    <Button
                      variant="ghost"
                      size="sm"
                      onPress={handleViewPatient}
                    >
                      <FaIcon icon="user" aria-hidden="true" className="mr-1.5" />
                      {t("clinic.appointments.agenda.viewPatient")}
                    </Button>
                  )}
                  <Button
                    variant="ghost"
                    size="sm"
                    onPress={() => setDeleting(true)}
                    isDisabled={!reservation}
                  >
                    <FaIcon icon="trash" aria-hidden="true" className="mr-1.5" />
                    {t("common.actions.delete")}
                  </Button>
                  {canConfirm && (
                    <>
                      <div className="h-4 w-px bg-divider" />
                      <Button
                        variant="primary"
                        size="sm"
                        onPress={handleConfirm}
                        isPending={updateStatus.isPending}
                      >
                        <FaIcon icon="check" aria-hidden="true" className="mr-1.5" />
                        {t("clinic.appointments.reservations.confirmReservation")}
                      </Button>
                    </>
                  )}
                </div>
              </Modal.Footer>
            </Modal.Dialog>
          </Modal.Container>
        </Modal.Backdrop>
      </Modal>

      <ReservationDeleteDialog
        isOpen={deleting}
        isPending={deletion.isPending}
        onCancel={() => setDeleting(false)}
        onConfirm={handleDelete}
      />
    </>
  );
}

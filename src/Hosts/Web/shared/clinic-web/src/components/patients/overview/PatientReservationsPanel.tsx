"use client";

import { useCallback, useMemo, useState } from "react";
import { Button, Chip } from "@heroui/react";
import { ItemCard, PressableFeedback } from "@heroui-pro/react";

import {
  EmptyStateSurface as ClinicEmptyState,
  ErrorStateSurface as ClinicErrorState,
  FaIcon,
  LoadingStateSurface as ClinicLoadingState,
} from "@vitarish/web-ui";
import { useLocalizedText, type MessageKey } from "../../../localization/index";
import { useOwnerReservations } from "../../../hooks/appointments/reservations/useOwnerReservations";
import { useOwnerShifts } from "../../../hooks/appointments/shifts/useOwnerShifts";
import { ReservationDetailDialog } from "../../appointments/agenda/ReservationDetailDialog";

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

interface PatientReservationsPanelProps {
  patientId: string;
}

export function PatientReservationsPanel({ patientId }: PatientReservationsPanelProps) {
  const { t } = useLocalizedText();
  const [detailReservationId, setDetailReservationId] = useState<string | null>(null);

  const { data: reservations, isLoading, error } = useOwnerReservations({
    reservingPartyId: patientId,
  });
  const { data: shifts } = useOwnerShifts();

  const shiftMap = useMemo(
    () => new Map(shifts?.map((s) => [s.id, s]) ?? []),
    [shifts],
  );

  const today = new Date().toISOString().split("T")[0];

  const sortedReservations = useMemo(() => {
    if (!reservations) return [];
    return [...reservations]
      .sort((a, b) => b.date.localeCompare(a.date))
      .slice(0, 20);
  }, [reservations]);

  const upcoming = useMemo(
    () => sortedReservations.filter((r) => r.date >= today),
    [sortedReservations, today],
  );
  const past = useMemo(
    () => sortedReservations.filter((r) => r.date < today).slice(0, 5),
    [sortedReservations, today],
  );

  const handleViewReservation = useCallback((reservationId: string) => {
    setDetailReservationId(reservationId);
  }, []);

  if (isLoading) {
    return <ClinicLoadingState label={t("common.states.loading")} />;
  }

  if (error) {
    return (
      <ClinicErrorState title={t("clinic.appointments.reservations.failedToLoad")} />
    );
  }

  if (sortedReservations.length === 0) {
    return (
      <ClinicEmptyState title={t("clinic.patient.reservations.empty")}>
        <p className="text-sm text-muted">
          {t("clinic.patient.reservations.emptyHint")}
        </p>
      </ClinicEmptyState>
    );
  }

  return (
    <div className="flex flex-col gap-4">
      {upcoming.length > 0 && (
        <section>
          <h3 className="mb-2 text-sm font-medium text-muted">
            {t("clinic.patient.reservations.upcoming")}
          </h3>
          <div className="flex flex-col gap-1.5">
            {upcoming.map((reservation) => (
              <ReservationRow
                key={reservation.id}
                reservation={reservation}
                shiftTime={shiftMap.get(reservation.shiftId)}
                onSelect={handleViewReservation}
              />
            ))}
          </div>
        </section>
      )}

      {past.length > 0 && (
        <section>
          <h3 className="mb-2 text-sm font-medium text-muted">
            {t("clinic.patient.reservations.recentPast")}
          </h3>
          <div className="flex flex-col gap-1.5">
            {past.map((reservation) => (
              <ReservationRow
                key={reservation.id}
                reservation={reservation}
                shiftTime={shiftMap.get(reservation.shiftId)}
                onSelect={handleViewReservation}
              />
            ))}
          </div>
        </section>
      )}

      {detailReservationId && (
        <ReservationDetailDialog
          isOpen={!!detailReservationId}
          onClose={() => setDetailReservationId(null)}
          reservationId={detailReservationId}
        />
      )}
    </div>
  );
}

/* ─── Reservation Row ─── */

interface ReservationRowProps {
  reservation: { id: string; date: string; status: number; isHold: boolean; shiftId: string };
  shiftTime: { startTime: string; endTime: string } | undefined;
  onSelect: (reservationId: string) => void;
}

function ReservationRow({ reservation, shiftTime, onSelect }: ReservationRowProps) {
  const { t } = useLocalizedText();

  return (
    <ItemCard<"button">
      className="relative w-full cursor-pointer overflow-hidden text-start"
      render={(props) => (
        <button
          type="button"
          {...props}
          onClick={(event) => {
            props.onClick?.(event);
            onSelect(reservation.id);
          }}
        />
      )}
      variant="outline"
    >
      <PressableFeedback.Highlight />
      <ItemCard.Icon>
        <FaIcon
          icon="calendar"
          aria-hidden="true"
          className="text-muted"
        />
      </ItemCard.Icon>
      <ItemCard.Content>
        <ItemCard.Title>
          {reservation.date}
          {shiftTime && (
            <span className="ml-2 text-sm font-normal text-muted">
              {shiftTime.startTime} – {shiftTime.endTime}
            </span>
          )}
        </ItemCard.Title>
        <ItemCard.Description>
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
        </ItemCard.Description>
      </ItemCard.Content>
      <ItemCard.Action>
        <FaIcon icon="chevron-right" aria-hidden="true" className="text-muted" />
      </ItemCard.Action>
    </ItemCard>
  );
}

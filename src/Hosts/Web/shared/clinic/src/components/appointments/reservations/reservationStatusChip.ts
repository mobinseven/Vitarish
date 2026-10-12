import { chromaChipClassName } from "@vitarish/ui/chroma";

/**
 * Values of the Appointments API's ReservationStatus enum
 * (src/Modules/Appointments/Contracts/Enums/ReservationStatus.cs).
 */
export const reservationStatuses = {
  pending: 0,
  confirmed: 1,
  cancelled: 2,
  rejected: 3,
  expired: 4,
  waitlisted: 5,
  present: 6,
  completed: 7,
} as const;

export type ReservationStatusChipProps = {
  color?: "accent" | "danger" | "success" | "warning";
  variant?: "soft";
  className?: string;
};

const neutral: ReservationStatusChipProps = {};

/**
 * Each status a user acts on has its own color, so statuses shown together
 * never share one; cancelled and rejected need no attention and stay neutral
 * (docs/hosts/web/color-roles.md). Every chip is a soft chip.
 */
const reservationStatusChips: Record<number, ReservationStatusChipProps> = {
  [reservationStatuses.pending]: { color: "warning", variant: "soft" },
  [reservationStatuses.confirmed]: { color: "success", variant: "soft" },
  [reservationStatuses.present]: { color: "accent", variant: "soft" },
  [reservationStatuses.completed]: { className: chromaChipClassName("primary") },
  [reservationStatuses.waitlisted]: { className: chromaChipClassName("tertiary") },
  [reservationStatuses.expired]: { color: "danger", variant: "soft" },
  [reservationStatuses.cancelled]: neutral,
  [reservationStatuses.rejected]: neutral,
};

/** Props that color a HeroUI Chip for a reservation status. */
export function reservationStatusChipProps(status: number): ReservationStatusChipProps {
  return reservationStatusChips[status] ?? neutral;
}

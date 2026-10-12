import { describe, expect, it } from "vitest";
import {
  reservationStatusChipProps,
  reservationStatuses,
} from "../components/appointments/reservations/reservationStatusChip";

const attentionStatuses = [
  reservationStatuses.pending,
  reservationStatuses.confirmed,
  reservationStatuses.present,
  reservationStatuses.completed,
  reservationStatuses.waitlisted,
  reservationStatuses.expired,
];

function colorOf(status: number): string {
  const { color, className } = reservationStatusChipProps(status);
  return color ?? className ?? "neutral";
}

describe("reservationStatusChipProps", () => {
  it("gives every status a user acts on its own color", () => {
    const colors = attentionStatuses.map(colorOf);

    expect(colors).not.toContain("neutral");
    expect(new Set(colors).size).toBe(attentionStatuses.length);
  });

  it("keeps statuses that need no attention, and unknown values, neutral", () => {
    expect(colorOf(reservationStatuses.cancelled)).toBe("neutral");
    expect(colorOf(reservationStatuses.rejected)).toBe("neutral");
    expect(colorOf(99)).toBe("neutral");
  });
});

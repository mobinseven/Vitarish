import type { Metadata } from "next";
import { OwnerReservationsList } from "@/features/clinic/appointments/reservations/components/OwnerReservationsList";

export const metadata: Metadata = {
  title: "Reservations | Vitarish",
  description: "Manage appointment reservations",
};

export default function ClinicOwnerReservationsPage() {
  return <OwnerReservationsList />;
}

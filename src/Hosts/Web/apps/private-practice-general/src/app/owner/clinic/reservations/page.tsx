import type { Metadata } from "next";
import { OwnerReservationsAgenda } from "@vitarish/clinic-web";

export const metadata: Metadata = {
  title: "Reservations | Vitarish",
  description: "Manage appointment reservations",
};

export default function ClinicOwnerReservationsPage() {
  return <OwnerReservationsAgenda />;
}

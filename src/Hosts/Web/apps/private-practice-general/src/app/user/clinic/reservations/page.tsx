import type { Metadata } from "next";
import { UserReservationsList } from "@vitarish/clinic-web";

export const metadata: Metadata = {
  title: "My Reservations | Vitarish",
  description: "View and manage your appointment reservations",
};

export default function ClinicUserReservationsPage() {
  return <UserReservationsList />;
}

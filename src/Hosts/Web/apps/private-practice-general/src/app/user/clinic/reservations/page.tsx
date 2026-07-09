import type { Metadata } from "next";
import { UserReservationsList } from "@/features/clinic/user-appointments/reservations/components/UserReservationsList";

export const metadata: Metadata = {
  title: "My Reservations | Vitarish",
  description: "View and manage your appointment reservations",
};

export default function ClinicUserReservationsPage() {
  return <UserReservationsList />;
}

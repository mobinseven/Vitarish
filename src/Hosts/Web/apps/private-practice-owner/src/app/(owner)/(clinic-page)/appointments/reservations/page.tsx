import { LegacyPaths, redirectLegacyRoute } from "@/app/legacyRoutes";

/** The retired address of the reservation list; see legacyRoutes. */
export default function LegacyReservationsRoute(): never {
  return redirectLegacyRoute(LegacyPaths.Reservations);
}

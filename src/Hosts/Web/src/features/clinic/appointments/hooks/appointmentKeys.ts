export const appointmentKeys = {
  all: ["clinic", "appointments"] as const,
  bookableDates: (shiftId: string, from: string, to: string) =>
    [...appointmentKeys.all, "bookable-dates", shiftId, from, to] as const,
  bookableSlots: (shiftId: string, date: string) =>
    [...appointmentKeys.all, "bookable-slots", shiftId, date] as const,
  shifts: ["clinic", "appointments", "shifts"] as const,
  shiftDetail: (shiftId: string) =>
    [...appointmentKeys.shifts, "detail", shiftId] as const,
  shiftOccurrence: (shiftId: string, date: string) =>
    [...appointmentKeys.shifts, "occurrence", shiftId, date] as const,
  reservations: ["clinic", "appointments", "reservations"] as const,
  reservationDetail: (reservationId: string) =>
    [...appointmentKeys.reservations, "detail", reservationId] as const,
  serviceTypes: ["clinic", "appointments", "service-types"] as const,
  serviceTypeDetail: (id: string) =>
    [...appointmentKeys.serviceTypes, "detail", id] as const,
  services: ["clinic", "appointments", "services"] as const,
  serviceDetail: (id: string) =>
    [...appointmentKeys.services, "detail", id] as const,
  serviceProviders: ["clinic", "appointments", "service-providers"] as const,
  serviceProviderDetail: (id: string) =>
    [...appointmentKeys.serviceProviders, "detail", id] as const,
};

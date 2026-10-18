import type { DashboardAppointmentResponse } from "../../api/generated/clinic/model";
import type { Translate } from "../../localization";
import { reservationStatuses } from "../../components/appointments/reservations/reservationStatusChip";

/** One visit row: a reservation with its patient and service, as the clinic dashboard query returns it. */
export type Visit = DashboardAppointmentResponse;

export type VisitSortMode = "time" | "patient";

export interface VisitFilters {
  search: string;
  statuses: string[];
  serviceKeys: string[];
}

export const emptyVisitFilters: VisitFilters = { search: "", statuses: [], serviceKeys: [] };

const patientIdPattern = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

/** The visit's patient id when it can address a patient record, otherwise null. */
export function visitPatientId(visit: Visit): string | null {
  return visit.patientId && patientIdPattern.test(visit.patientId) ? visit.patientId : null;
}

export function visitPatientLabel(visit: Visit, t: Translate): string {
  return visit.patientDisplayName?.trim() || t("clinic.appointments.visits.patientUnavailable");
}

export function visitServiceLabel(visit: Visit, locale: string, t: Translate): string {
  const service = visit.serviceDisplayName?.trim();
  if (!service) return t("clinic.appointments.visits.serviceUnavailable");
  if (locale.startsWith("fa")) {
    if (service === "Initial consultation") return t("clinic.appointments.visits.initialConsultation");
    if (service === "Follow-up visit") return t("clinic.appointments.visits.followUpVisit");
  }
  return service;
}

const statusLabelKeys: Record<number, string> = {
  [reservationStatuses.pending]: "clinic.reservationStatus.pending",
  [reservationStatuses.confirmed]: "clinic.reservationStatus.confirmed",
  [reservationStatuses.cancelled]: "clinic.reservationStatus.cancelled",
  [reservationStatuses.rejected]: "clinic.reservationStatus.cancelled",
  [reservationStatuses.expired]: "clinic.reservationStatus.expired",
  [reservationStatuses.waitlisted]: "clinic.reservationStatus.waitlisted",
  [reservationStatuses.present]: "clinic.reservationStatus.present",
  [reservationStatuses.completed]: "clinic.reservationStatus.completed",
};

export function visitStatusLabel(status: number, t: Translate): string {
  return t(statusLabelKeys[status] ?? "clinic.appointments.visits.unknownStatus");
}

/** A key that groups visits by service, by id when the service has one. */
export function visitServiceKey(visit: Visit, locale: string): string {
  if (visit.serviceId?.trim()) return `id:${visit.serviceId.trim()}`;
  if (visit.serviceDisplayName?.trim()) return `name:${visit.serviceDisplayName.trim().toLocaleLowerCase(locale)}`;
  return "missing-service";
}

/** The visit's start as a sortable number; a visit without a time sorts first on its day. */
export function visitStartValue(visit: Visit): number {
  return Date.parse(`${visit.date}T${visit.startTime ?? "00:00:00"}`);
}

/** Visits matching the filters, in the chosen order. */
export function selectVisits(
  visits: Visit[],
  filters: VisitFilters,
  sortMode: VisitSortMode,
  locale: string,
  t: Translate,
): Visit[] {
  const query = filters.search.trim().toLocaleLowerCase(locale);
  const statuses = new Set(filters.statuses);
  const serviceKeys = new Set(filters.serviceKeys);

  return visits
    .filter((visit) => {
      if (statuses.size > 0 && !statuses.has(String(visit.status))) return false;
      if (serviceKeys.size > 0 && !serviceKeys.has(visitServiceKey(visit, locale))) return false;
      if (!query) return true;
      return [visit.patientDisplayName, visit.serviceDisplayName, visit.reservationId]
        .some((value) => value?.toLocaleLowerCase(locale).includes(query));
    })
    .sort((left, right) =>
      sortMode === "patient"
        ? visitPatientLabel(left, t).localeCompare(visitPatientLabel(right, t), locale)
        : visitStartValue(left) - visitStartValue(right),
    );
}

export function activeVisitFilterCount(filters: VisitFilters): number {
  return filters.statuses.length + filters.serviceKeys.length + (filters.search.trim() ? 1 : 0);
}

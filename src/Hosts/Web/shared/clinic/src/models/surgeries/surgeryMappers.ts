import type {
  SurgeryCalendarEntryResponse,
  SurgeryCatalogDto,
  SurgeryConflictResponse,
  SurgeryContactDto,
  SurgeryCreateRequest,
  SurgeryReminderPolicyDto,
  SurgeryResponse,
  SurgerySettingsDto,
  SurgerySuggestionsResponse,
  SurgeryTeamMemberDto,
  SurgeryWriteRequest,
} from "../../api/generated/clinic/model";
import {
  surgeryConflictReasons,
  surgeryStatuses,
  type ClinicStaffMember,
  type Surgery,
  type SurgeryConflict,
  type SurgeryConflictReason,
  type SurgeryContact,
  type SurgeryReminderPolicy,
  type SurgerySettings,
  type SurgeryStatus,
  type SurgerySuggestions,
  type SurgeryTeamMember,
  type SurgeryType,
} from "./surgeryModels";
import {
  scheduledInstant,
  type SurgeryPlanFormValues,
  type SurgeryPlanTeamRow,
} from "./surgeryPlanForm";

function mapSurgeryStatus(status: string): SurgeryStatus {
  if (status === surgeryStatuses.planned) return surgeryStatuses.planned;
  if (status === surgeryStatuses.completed) return surgeryStatuses.completed;
  if (status === surgeryStatuses.cancelled) return surgeryStatuses.cancelled;
  throw new Error(`Unknown surgery status: ${status}`);
}

function mapSurgeryConflictReason(reason: string): SurgeryConflictReason {
  if (reason === surgeryConflictReasons.sameLocation) {
    return surgeryConflictReasons.sameLocation;
  }
  if (reason === surgeryConflictReasons.sharedTeamMember) {
    return surgeryConflictReasons.sharedTeamMember;
  }
  throw new Error(`Unknown surgery conflict reason: ${reason}`);
}

function mapDurationMinutes(
  value: number | string | null | undefined,
): number | undefined {
  if (value === undefined || value === null) return undefined;
  const parsed = Number(value);
  if (!Number.isFinite(parsed) || parsed < 0) {
    throw new Error(`Invalid surgery duration: ${String(value)}`);
  }
  return parsed;
}

function mapReminderPolicy(dto: SurgeryReminderPolicyDto): SurgeryReminderPolicy {
  const minutesBefore = Number(dto.minutesBefore);
  if (!Number.isFinite(minutesBefore) || minutesBefore < 0) {
    throw new Error(`Invalid surgery reminder offset: ${String(dto.minutesBefore)}`);
  }
  return {
    name: dto.name,
    minutesBefore,
    subject: dto.subject,
    textBody: dto.textBody,
  };
}

function mapTeamMember(dto: SurgeryTeamMemberDto): SurgeryTeamMember {
  return { userId: dto.userId, displayName: dto.displayName, role: dto.role };
}

function mapContact(dto: SurgeryContactDto): SurgeryContact {
  return { name: dto.name, destination: dto.destination, channel: dto.channel };
}

export function mapSurgeryResponse(dto: SurgeryResponse): Surgery {
  return {
    id: dto.id,
    patientId: dto.patientId,
    catalogId: dto.catalogId,
    name: dto.name,
    status: mapSurgeryStatus(dto.status),
    scheduledAt: dto.scheduledAt,
    durationMinutes: mapDurationMinutes(dto.durationMinutes),
    location: dto.location ?? undefined,
    team: dto.team.map(mapTeamMember),
    tools: [...dto.tools],
    personsToNotify: [...dto.personsToNotify],
    contacts: dto.contacts.map(mapContact),
    reminderPolicies: dto.reminderPolicies.map(mapReminderPolicy),
    concurrencyToken: dto.concurrencyToken,
    createdAt: dto.createdAt,
    updatedAt: dto.updatedAt ?? undefined,
  };
}

export function mapSurgeryCalendarEntry(dto: SurgeryCalendarEntryResponse): Surgery {
  return {
    ...mapSurgeryResponse(dto.surgery),
    patientDisplayName: dto.patientDisplayName ?? undefined,
  };
}

export function mapSurgeryType(dto: SurgeryCatalogDto): SurgeryType {
  return {
    id: dto.id,
    name: dto.name,
    tools: [...dto.tools],
    defaultTeamRoles: [...dto.defaultTeamRoles],
    reminderPolicies: dto.reminderPolicies.map(mapReminderPolicy),
    concurrencyToken: dto.concurrencyToken,
  };
}

export function mapSurgerySettings(dto: SurgerySettingsDto): SurgerySettings {
  return {
    cancellationSubject: dto.cancellationSubject,
    cancellationTextBody: dto.cancellationTextBody,
    concurrencyToken: dto.concurrencyToken,
  };
}

export function mapSurgeryConflict(dto: SurgeryConflictResponse): SurgeryConflict {
  return {
    surgery: mapSurgeryCalendarEntry(dto.entry),
    reasons: dto.reasons.map(mapSurgeryConflictReason),
    sharedTeamUserIds: [...dto.sharedTeamUserIds],
  };
}

export function mapSurgerySuggestions(dto: SurgerySuggestionsResponse): SurgerySuggestions {
  return { locations: [...dto.locations], roles: [...dto.roles] };
}

export interface SurgeryRequestStaffContext {
  timeZone: string;
  staff: readonly ClinicStaffMember[];
}

type SurgeryRequestCoreFields = Pick<
  SurgeryCreateRequest,
  | "catalogId"
  | "name"
  | "status"
  | "scheduledAt"
  | "tools"
  | "reminderPolicies"
  | "durationMinutes"
  | "location"
>;

function buildTeam(
  rows: readonly SurgeryPlanTeamRow[],
  staff: readonly ClinicStaffMember[],
): SurgeryTeamMemberDto[] {
  return rows
    .filter((row) => row.userId.trim().length > 0)
    .map((row) => ({
      userId: row.userId,
      displayName:
        staff.find((member) => member.userId === row.userId)?.displayName ?? "",
      role: row.role.trim(),
    }));
}

function buildCoreFields(
  values: SurgeryPlanFormValues,
  timeZone: string,
): SurgeryRequestCoreFields {
  const scheduledAt = scheduledInstant(values, timeZone);
  if (!scheduledAt) {
    throw new Error("The surgery schedule is incomplete.");
  }

  const durationMinutes = values.durationMinutes.trim();
  const location = values.location.trim();

  return {
    catalogId: values.catalogId,
    name: values.name.trim(),
    status: values.status,
    scheduledAt,
    tools: [...values.tools],
    reminderPolicies: values.reminderPolicies.map((policy) => ({
      name: policy.name,
      minutesBefore: policy.minutesBefore,
      subject: policy.subject,
      textBody: policy.textBody,
    })),
    durationMinutes: durationMinutes ? Number(durationMinutes) : null,
    location: location.length > 0 ? location : null,
  };
}

export function buildSurgeryCreateRequest(
  values: SurgeryPlanFormValues,
  context: SurgeryRequestStaffContext & { idempotencyKey: string },
): SurgeryCreateRequest {
  return {
    ...buildCoreFields(values, context.timeZone),
    // A new surgery always starts planned.
    status: surgeryStatuses.planned,
    personsToNotify: [],
    contacts: [],
    idempotencyKey: context.idempotencyKey,
    team: buildTeam(values.team, context.staff),
  };
}

export function buildSurgeryWriteRequest(
  surgery: Surgery,
  values: SurgeryPlanFormValues,
  context: SurgeryRequestStaffContext,
): SurgeryWriteRequest {
  return {
    ...buildCoreFields(values, context.timeZone),
    personsToNotify: [...surgery.personsToNotify],
    contacts: surgery.contacts.map((contact) => ({ ...contact })),
    concurrencyToken: surgery.concurrencyToken,
    team: buildTeam(values.team, context.staff),
  };
}

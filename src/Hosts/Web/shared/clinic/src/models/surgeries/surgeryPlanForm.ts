import {
  CalendarDateTime,
  parseAbsolute,
  toCalendarDate,
  toZoned,
} from "@internationalized/date";
import { surgeryStatuses, type Surgery, type SurgeryReminderPolicy, type SurgeryStatus, type SurgeryType } from "./surgeryModels";

export interface SurgeryPlanTeamRow {
  key: string;
  userId: string;
  role: string;
}

export interface SurgeryPlanFormValues {
  patientId: string;
  catalogId: string;
  name: string;
  /** ISO date (yyyy-mm-dd) in the clinic's calendar. */
  scheduledDate: string;
  /** 24-hour HH:mm. */
  scheduledTime: string;
  /** Empty when the duration is unknown. */
  durationMinutes: string;
  location: string;
  team: SurgeryPlanTeamRow[];
  tools: string[];
  reminderPolicies: SurgeryReminderPolicy[];
  /** Planned or completed; only an edit of a completed surgery offers the choice. */
  status: SurgeryStatus;
}

export type SurgeryPlanField = keyof SurgeryPlanFormValues;

/** Field -> localization key, so the form can render the message with `t`. */
export type SurgeryPlanErrors = Partial<Record<SurgeryPlanField, string>>;

const maxLocationLength = 200;
const maxRoleLength = 100;
const minDurationMinutes = 1;
const maxDurationMinutes = 1440;
const isoDatePattern = /^(\d{4})-(\d{2})-(\d{2})$/;
const clockTimePattern = /^(\d{1,2}):(\d{2})$/;

let teamRowKeyCounter = 0;

function newRowKey(): string {
  if (typeof crypto !== "undefined" && typeof crypto.randomUUID === "function") {
    return crypto.randomUUID();
  }
  teamRowKeyCounter += 1;
  return `surgery-team-row-${teamRowKeyCounter}`;
}

function pad(value: number, length: number): string {
  return String(value).padStart(length, "0");
}

function isValidIsoDate(value: string): boolean {
  const match = isoDatePattern.exec(value);
  if (!match) return false;
  const month = Number(match[2]);
  const day = Number(match[3]);
  return month >= 1 && month <= 12 && day >= 1 && day <= 31;
}

function isValidClockTime(value: string): boolean {
  const match = clockTimePattern.exec(value);
  if (!match) return false;
  const hour = Number(match[1]);
  const minute = Number(match[2]);
  return hour <= 23 && minute <= 59;
}

export function emptySurgeryPlanForm(patientId: string): SurgeryPlanFormValues {
  return {
    patientId,
    catalogId: "",
    name: "",
    scheduledDate: "",
    scheduledTime: "",
    durationMinutes: "",
    location: "",
    team: [],
    tools: [],
    reminderPolicies: [],
    status: surgeryStatuses.planned,
  };
}

/** Fills the plan with a surgery type's defaults, keeping everything already entered. */
export function applySurgeryType(
  values: SurgeryPlanFormValues,
  type: SurgeryType,
): SurgeryPlanFormValues {
  const existingRoles = new Set(
    values.team.map((row) => row.role.trim()).filter((role) => role.length > 0),
  );
  const additions: SurgeryPlanTeamRow[] = [];
  for (const role of type.defaultTeamRoles) {
    const trimmed = role.trim();
    if (!trimmed || existingRoles.has(trimmed)) continue;
    existingRoles.add(trimmed);
    additions.push({ key: newRowKey(), userId: "", role: trimmed });
  }

  return {
    ...values,
    catalogId: type.id,
    name: values.name.trim() ? values.name : type.name,
    tools: [...type.tools],
    reminderPolicies: type.reminderPolicies.map((policy) => ({ ...policy })),
    team: [...values.team, ...additions],
  };
}

/** Rebuilds the editable plan from a stored surgery. */
export function surgeryPlanFormFromSurgery(
  surgery: Surgery,
  timeZone: string,
): SurgeryPlanFormValues {
  const zoned = parseAbsolute(surgery.scheduledAt, timeZone);

  return {
    patientId: surgery.patientId,
    catalogId: surgery.catalogId,
    name: surgery.name,
    scheduledDate: toCalendarDate(zoned).toString(),
    scheduledTime: `${pad(zoned.hour, 2)}:${pad(zoned.minute, 2)}`,
    durationMinutes:
      surgery.durationMinutes === undefined
        ? ""
        : String(surgery.durationMinutes),
    location: surgery.location ?? "",
    team: surgery.team.map((member) => ({
      key: newRowKey(),
      userId: member.userId,
      role: member.role,
    })),
    tools: [...surgery.tools],
    reminderPolicies: surgery.reminderPolicies.map((policy) => ({ ...policy })),
    status: surgery.status,
  };
}

/** Combines the plan's date and time in the clinic time zone into an ISO instant. */
export function scheduledInstant(
  values: Pick<SurgeryPlanFormValues, "scheduledDate" | "scheduledTime">,
  timeZone: string,
): string | undefined {
  const dateMatch = isoDatePattern.exec(values.scheduledDate.trim());
  if (!dateMatch || !isValidIsoDate(values.scheduledDate.trim())) return undefined;

  const timeMatch = clockTimePattern.exec(values.scheduledTime.trim());
  if (!timeMatch || !isValidClockTime(values.scheduledTime.trim())) return undefined;

  try {
    const dateTime = new CalendarDateTime(
      Number(dateMatch[1]),
      Number(dateMatch[2]),
      Number(dateMatch[3]),
      Number(timeMatch[1]),
      Number(timeMatch[2]),
      0,
    );
    return toZoned(dateTime, timeZone).toAbsoluteString();
  } catch {
    return undefined;
  }
}

/**
 * Returns the first problem per field, as localization keys. With a schedule context, a surgery
 * marked completed must already have started.
 */
export function validateSurgeryPlanForm(
  values: SurgeryPlanFormValues,
  schedule?: { timeZone: string; now: Date },
): SurgeryPlanErrors {
  const errors: SurgeryPlanErrors = {};

  if (values.status === surgeryStatuses.completed && schedule) {
    const start = scheduledInstant(values, schedule.timeZone);
    if (start && new Date(start).getTime() > schedule.now.getTime()) {
      errors.status = "clinic.surgeries.validation.completedInFuture";
    }
  }

  if (!values.patientId.trim()) {
    errors.patientId = "clinic.surgeries.validation.patientRequired";
  }
  if (!values.catalogId.trim()) {
    errors.catalogId = "clinic.surgeries.validation.typeRequired";
  }
  if (!values.name.trim()) {
    errors.name = "clinic.surgeries.validation.nameRequired";
  }

  const date = values.scheduledDate.trim();
  if (!date) errors.scheduledDate = "clinic.surgeries.validation.dateRequired";
  else if (!isValidIsoDate(date)) {
    errors.scheduledDate = "clinic.surgeries.validation.dateInvalid";
  }

  const time = values.scheduledTime.trim();
  if (!time) errors.scheduledTime = "clinic.surgeries.validation.timeRequired";
  else if (!isValidClockTime(time)) {
    errors.scheduledTime = "clinic.surgeries.validation.timeInvalid";
  }

  const duration = values.durationMinutes.trim();
  if (duration) {
    const minutes = Number(duration);
    if (
      !Number.isInteger(minutes) ||
      minutes < minDurationMinutes ||
      minutes > maxDurationMinutes
    ) {
      errors.durationMinutes = "clinic.surgeries.validation.durationInvalid";
    }
  }

  if (values.location.trim().length > maxLocationLength) {
    errors.location = "clinic.surgeries.validation.locationTooLong";
  }

  const seenUsers = new Set<string>();
  let duplicateUser = false;
  let teamRoleError: string | undefined;
  for (const row of values.team) {
    const userId = row.userId.trim();
    if (!userId) continue;
    if (seenUsers.has(userId)) duplicateUser = true;
    seenUsers.add(userId);

    const role = row.role.trim();
    if (!role) {
      teamRoleError ??= "clinic.surgeries.validation.teamRoleRequired";
    } else if (role.length > maxRoleLength) {
      teamRoleError ??= "clinic.surgeries.validation.teamRoleTooLong";
    }
  }
  if (duplicateUser) errors.team = "clinic.surgeries.validation.teamDuplicate";
  else if (teamRoleError) errors.team = teamRoleError;

  for (const reminder of values.reminderPolicies) {
    if (!Number.isInteger(reminder.minutesBefore) || reminder.minutesBefore < 0) {
      errors.reminderPolicies = "clinic.surgeries.validation.reminderMinutesInvalid";
      break;
    }
    if (!reminder.subject.trim()) {
      errors.reminderPolicies = "clinic.surgeries.validation.reminderSubjectRequired";
      break;
    }
    if (!reminder.textBody.trim()) {
      errors.reminderPolicies = "clinic.surgeries.validation.reminderMessageRequired";
      break;
    }
  }

  return errors;
}

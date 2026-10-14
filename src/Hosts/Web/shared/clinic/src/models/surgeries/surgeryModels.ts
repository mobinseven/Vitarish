/** The surgery statuses the clinic API reports. */
export const surgeryStatuses = {
  planned: "Planned",
  completed: "Completed",
  cancelled: "Cancelled",
} as const;

export type SurgeryStatus = (typeof surgeryStatuses)[keyof typeof surgeryStatuses];

export interface SurgeryTeamMember {
  userId: string;
  displayName: string;
  role: string;
}

export interface SurgeryReminderPolicy {
  name: string;
  minutesBefore: number;
  subject: string;
  textBody: string;
}

export interface Surgery {
  id: string;
  patientId: string;
  patientDisplayName?: string;
  catalogId: string;
  name: string;
  status: SurgeryStatus;
  /** ISO instant. */
  scheduledAt: string;
  durationMinutes?: number;
  location?: string;
  team: SurgeryTeamMember[];
  tools: string[];
  reminderPolicies: SurgeryReminderPolicy[];
  concurrencyToken: string;
  createdAt: string;
  updatedAt?: string;
}

export interface SurgeryType {
  id: string;
  name: string;
  tools: string[];
  defaultTeamRoles: string[];
  reminderPolicies: SurgeryReminderPolicy[];
  concurrencyToken: string;
}

/** The clinic's cancellation message, sent to the patient and surgical team. */
export interface SurgerySettings {
  cancellationSubject: string;
  cancellationTextBody: string;
  concurrencyToken: string;
}

/** The reasons the conflict check reports, as the clinic API names them. */
export const surgeryConflictReasons = {
  sameLocation: "SameLocation",
  sharedTeamMember: "SharedTeamMember",
} as const;

export type SurgeryConflictReason =
  (typeof surgeryConflictReasons)[keyof typeof surgeryConflictReasons];

export interface SurgeryConflict {
  surgery: Surgery;
  reasons: SurgeryConflictReason[];
  sharedTeamUserIds: string[];
}

export interface SurgerySuggestions {
  locations: string[];
  roles: string[];
}

export interface ClinicStaffMember {
  userId: string;
  displayName: string;
}

/** The probe the conflict check runs against; every field is optional except the team. */
export interface SurgeryConflictProbe {
  scheduledAt?: string;
  durationMinutes?: number;
  location?: string;
  teamUserIds: string[];
  excludeSurgeryId?: string;
}

/** The end of a surgery, when a duration is set. */
export function surgeryEnd(
  surgery: Pick<Surgery, "scheduledAt" | "durationMinutes">,
): Date | undefined {
  if (surgery.durationMinutes === undefined) {
    return undefined;
  }

  const start = new Date(surgery.scheduledAt);
  if (Number.isNaN(start.getTime())) return undefined;
  return new Date(start.getTime() + surgery.durationMinutes * 60_000);
}

/** Only planned surgeries can still be changed. */
export function canEditSurgery(surgery: Surgery): boolean {
  return surgery.status === surgeryStatuses.planned;
}

/** A planned surgery can be completed once its scheduled start has passed. */
export function canCompleteSurgery(surgery: Surgery, now: Date): boolean {
  if (surgery.status !== surgeryStatuses.planned) return false;
  const start = new Date(surgery.scheduledAt);
  if (Number.isNaN(start.getTime())) return false;
  return start.getTime() <= now.getTime();
}

/** Planned surgeries can be cancelled, and a completed one can be corrected. */
export function canCancelSurgery(surgery: Surgery): boolean {
  return (
    surgery.status === surgeryStatuses.planned ||
    surgery.status === surgeryStatuses.completed
  );
}

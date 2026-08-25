import { apiFetch } from "../client/apiFetch";

const BASE = "/communications";

export interface ReminderReadDto {
  id: string;
  sourceId: string;
  kind: number;
  recipientUserId?: string;
  recipient?: string;
  subject?: string;
  textBody?: string;
  remindAt: string;
  sentAt?: string;
  isSent: boolean;
  createdBy?: string;
  createdAt: string;
}

export interface ReminderDescriptorDto {
  sourceId?: string;
  isSent?: boolean;
}

export interface ReminderWriteDto {
  id?: string;
  sourceId: string;
  kind: number;
  recipientUserId?: string;
  recipient?: string;
  subject?: string;
  textBody?: string;
  remindAt: string;
}

export interface CommunicationsCommandResult {
  succeeded: boolean;
  entityId?: string;
}

export async function listReminders(descriptor: ReminderDescriptorDto = {}): Promise<ReminderReadDto[]> {
  return apiFetch<ReminderReadDto[]>({
    url: `${BASE}/reminders/query`,
    method: "POST",
    data: descriptor,
  });
}

export async function getRemindersBySource(sourceId: string): Promise<ReminderReadDto[]> {
  return apiFetch<ReminderReadDto[]>({
    url: `${BASE}/reminders/source/${sourceId}`,
    method: "GET",
  });
}

export async function createReminder(dto: ReminderWriteDto): Promise<CommunicationsCommandResult> {
  return apiFetch<CommunicationsCommandResult>({
    url: `${BASE}/reminders`,
    method: "POST",
    data: dto,
  });
}

export async function updateReminder(dto: ReminderWriteDto): Promise<CommunicationsCommandResult> {
  return apiFetch<CommunicationsCommandResult>({
    url: `${BASE}/reminders/${dto.id}`,
    method: "PUT",
    data: dto,
  });
}

export async function deleteReminder(id: string): Promise<CommunicationsCommandResult> {
  return apiFetch<CommunicationsCommandResult>({
    url: `${BASE}/reminders/${id}`,
    method: "DELETE",
  });
}

export async function markReminderSent(id: string, sentAt: string): Promise<CommunicationsCommandResult> {
  return apiFetch<CommunicationsCommandResult>({
    url: `${BASE}/reminders/${id}/sent`,
    method: "POST",
    data: { sentAt },
  });
}

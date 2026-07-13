import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { listReminders, createReminder, updateReminder, deleteReminder, markReminderSent, type ReminderDescriptorDto, type ReminderWriteDto } from "../../api/reminders/remindersApi";

const REMINDER_KEYS = ["clinic", "reminders"] as const;

export function useClinicOwnerReminders(descriptor: ReminderDescriptorDto = {}) {
  return useQuery({
    queryKey: [...REMINDER_KEYS, descriptor],
    queryFn: () => listReminders(descriptor),
  });
}

export function useCreateReminder() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (dto: ReminderWriteDto) => createReminder(dto),
    onSuccess: () => qc.invalidateQueries({ queryKey: REMINDER_KEYS }),
  });
}

export function useUpdateReminder() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (dto: ReminderWriteDto) => updateReminder(dto),
    onSuccess: () => qc.invalidateQueries({ queryKey: REMINDER_KEYS }),
  });
}

export function useDeleteReminder() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: deleteReminder,
    onSuccess: () => qc.invalidateQueries({ queryKey: REMINDER_KEYS }),
  });
}

export function useMarkReminderSent() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, sentAt }: { id: string; sentAt: string }) => markReminderSent(id, sentAt),
    onSuccess: () => qc.invalidateQueries({ queryKey: REMINDER_KEYS }),
  });
}

import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { useCallback } from "react";
// TODO: Import generated hooks after running Orval
// import { usePlatformListLocaleNotifications, usePlatformGetLocaleNotification, usePlatformCreateLocaleNotification, usePlatformUpdateLocaleNotification } from "../../api/generated/platform";

export interface LocaleListItem {
  normalizedLocale: string;
  locale: string;
}

export interface LocaleNotificationRecord {
  normalizedLocale: string;
  locale: string;
  verificationSmsTemplateId: string;
  seaReminderEmailSubject: string;
  seaReminderEmailTextBody: string;
  seaReminderEmailHtmlBody: string;
  seaReminderSmsTemplateId: string;
  appointmentReminderEmailSubject: string;
  appointmentReminderEmailTextBody: string;
  appointmentReminderEmailHtmlBody: string;
  appointmentReminderSmsTemplateId: string;
}

export interface LocaleNotificationRecordRequest {
  locale: string;
  verificationSmsTemplateId: string;
  seaReminderEmailSubject: string;
  seaReminderEmailTextBody: string;
  seaReminderEmailHtmlBody: string;
  seaReminderSmsTemplateId: string;
  appointmentReminderEmailSubject: string;
  appointmentReminderEmailTextBody: string;
  appointmentReminderEmailHtmlBody: string;
  appointmentReminderSmsTemplateId: string;
}

export const platformNotificationKeys = {
  all: ["platform", "notifications"] as const,
  lists: () => [...platformNotificationKeys.all, "list"] as const,
  detail: (normalizedLocale: string) =>
    [...platformNotificationKeys.all, "detail", normalizedLocale] as const,
};

// Placeholder hooks - replace with generated hooks after Orval generation
export function useLocaleNotificationList() {
  // TODO: Replace with usePlatformListLocaleNotifications()
  return useQuery({
    queryKey: platformNotificationKeys.lists(),
    queryFn: async (): Promise<LocaleListItem[]> => {
      // Placeholder - will be replaced by generated client
      return [];
    },
  });
}

export function useLocaleNotificationDetail(normalizedLocale: string) {
  // TODO: Replace with usePlatformGetLocaleNotification(normalizedLocale)
  return useQuery({
    queryKey: platformNotificationKeys.detail(normalizedLocale),
    queryFn: async (): Promise<LocaleNotificationRecord | null> => {
      // Placeholder - will be replaced by generated client
      return null;
    },
    enabled: !!normalizedLocale,
  });
}

export function useCreateLocaleNotification() {
  const queryClient = useQueryClient();
  // TODO: Replace with usePlatformCreateLocaleNotification()
  return useMutation({
    mutationFn: async (request: LocaleNotificationRecordRequest): Promise<void> => {
      // Placeholder - will be replaced by generated client
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: platformNotificationKeys.lists() });
    },
  });
}

export function useUpdateLocaleNotification(normalizedLocale: string) {
  const queryClient = useQueryClient();
  // TODO: Replace with usePlatformUpdateLocaleNotification(normalizedLocale)
  return useMutation({
    mutationFn: async (request: LocaleNotificationRecordRequest): Promise<void> => {
      // Placeholder - will be replaced by generated client
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: platformNotificationKeys.lists() });
      queryClient.invalidateQueries({ queryKey: platformNotificationKeys.detail(normalizedLocale) });
    },
  });
}

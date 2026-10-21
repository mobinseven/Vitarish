import { formatNumber, type SupportedLocale, type Translate } from "../../localization";

/** When a reminder goes out, in the largest whole unit before the scheduled start. */
export function surgeryReminderOffsetLabel(minutesBefore: number, t: Translate, locale: SupportedLocale): string {
  if (!Number.isFinite(minutesBefore) || minutesBefore <= 0) {
    return t("clinic.surgeries.detail.reminders.atStart");
  }
  if (minutesBefore % 1440 === 0) {
    return t("clinic.surgeries.detail.reminders.daysBefore", {
      count: formatNumber(minutesBefore / 1440, locale),
    });
  }
  if (minutesBefore % 60 === 0) {
    return t("clinic.surgeries.detail.reminders.hoursBefore", {
      count: formatNumber(minutesBefore / 60, locale),
    });
  }
  return t("clinic.surgeries.detail.reminders.minutesBefore", {
    count: formatNumber(minutesBefore, locale),
  });
}

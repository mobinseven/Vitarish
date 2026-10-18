import type { Translate } from "../../localization";

/** When a reminder goes out, in the largest whole unit before the scheduled start. */
export function surgeryReminderOffsetLabel(minutesBefore: number, t: Translate): string {
  if (!Number.isFinite(minutesBefore) || minutesBefore <= 0) {
    return t("clinic.surgeries.detail.reminders.atStart");
  }
  if (minutesBefore % 1440 === 0) {
    return t("clinic.surgeries.detail.reminders.daysBefore", {
      count: String(minutesBefore / 1440),
    });
  }
  if (minutesBefore % 60 === 0) {
    return t("clinic.surgeries.detail.reminders.hoursBefore", {
      count: String(minutesBefore / 60),
    });
  }
  return t("clinic.surgeries.detail.reminders.minutesBefore", {
    count: String(minutesBefore),
  });
}

import { describe, expect, it } from "vitest";
import type { MessageKey, MessageParameters } from "../localization";
import { surgeryReminderOffsetLabel } from "../models/surgeries/surgeryReminderOffset";

const t = (key: MessageKey, parameters?: MessageParameters) => `${key}:${String(parameters?.count ?? "")}`;

describe("surgeryReminderOffsetLabel", () => {
  it("writes the offset in the locale's digits", () => {
    expect(surgeryReminderOffsetLabel(120, t, "fa")).toBe("clinic.surgeries.detail.reminders.hoursBefore:۲");
    expect(surgeryReminderOffsetLabel(2880, t, "fa")).toBe("clinic.surgeries.detail.reminders.daysBefore:۲");
    expect(surgeryReminderOffsetLabel(90, t, "fa")).toBe("clinic.surgeries.detail.reminders.minutesBefore:۹۰");
    expect(surgeryReminderOffsetLabel(90, t, "en")).toBe("clinic.surgeries.detail.reminders.minutesBefore:90");
  });
});

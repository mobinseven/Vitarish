import { describe, expect, it } from "vitest";
import { getMessages, supportedLocales } from "@/localization";

function collectStrings(value: unknown, path = ""): string[] {
  if (typeof value === "string") return [path];
  if (!value || typeof value !== "object") return [];
  return Object.entries(value).flatMap(([key, child]) => collectStrings(child, path ? `${path}.${key}` : key));
}

describe("localization messages", () => {
  it("provides non-empty messages for every supported locale", () => {
    for (const locale of supportedLocales) {
      const messages = getMessages(locale);
      for (const key of collectStrings(messages)) {
        const value = key.split(".").reduce<unknown>((current, part) => current && typeof current === "object" ? (current as Record<string, unknown>)[part] : undefined, messages);
        expect(value, key).toBeTruthy();
      }
    }
  });

  it("includes plural variants for patient count", () => {
    const messages = getMessages("en");
    expect(messages.clinic.patients.countFound.one).toContain("{count}");
    expect(messages.clinic.patients.countFound.other).toContain("{count}");
  });

  it.each(["en", "fa"] as const)("composes package-owned clinic messages for %s", (locale) => {
    const messages = getMessages(locale);
    expect(messages.clinic.dashboard.title).toBeTruthy();
    expect(messages.clinic.appointments.agenda.allDay).toBeTruthy();
    expect(messages.clinic.services.deleteTitle).toBeTruthy();
  });
});

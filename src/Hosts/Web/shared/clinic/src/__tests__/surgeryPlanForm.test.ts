import { describe, expect, it } from "vitest";
import {
  applySurgeryType,
  emptySurgeryPlanForm,
  scheduledInstant,
  surgeryPlanFormFromSurgery,
  validateSurgeryPlanForm,
  type SurgeryPlanFormValues,
} from "../models/surgeries/surgeryPlanForm";
import {
  buildSurgeryCreateRequest,
  buildSurgeryWriteRequest,
} from "../models/surgeries/surgeryMappers";
import type { Surgery, SurgeryType } from "../models/surgeries/surgeryModels";

const typeFixture: SurgeryType = {
  id: "type-1",
  name: "Appendectomy",
  tools: ["Scalpel"],
  defaultTeamRoles: ["Surgeon", "Assistant"],
  reminderPolicies: [
    { name: "Reminder", minutesBefore: 1440, subject: "Prep", textBody: "Prepare" },
  ],
  concurrencyToken: "catalog-token",
};

function validValues(
  overrides: Partial<SurgeryPlanFormValues> = {},
): SurgeryPlanFormValues {
  return {
    ...emptySurgeryPlanForm("patient-1"),
    catalogId: typeFixture.id,
    name: "Knee replacement",
    scheduledDate: "2026-05-01",
    scheduledTime: "09:00",
    ...overrides,
  };
}

describe("validateSurgeryPlanForm", () => {
  it("accepts a complete plan", () => {
    expect(validateSurgeryPlanForm(validValues())).toEqual({});
  });

  it("rejects a team that repeats the same member", () => {
    const errors = validateSurgeryPlanForm(
      validValues({
        team: [
          { key: "row-1", userId: "user-1", role: "Surgeon" },
          { key: "row-2", userId: "user-1", role: "Assistant" },
        ],
      }),
    );

    expect(errors.team).toBe("clinic.surgeries.validation.teamDuplicate");
  });

  it("rejects a team row that has a member but no role", () => {
    const errors = validateSurgeryPlanForm(
      validValues({ team: [{ key: "row-1", userId: "user-1", role: "   " }] }),
    );

    expect(errors.team).toBe("clinic.surgeries.validation.teamRoleRequired");
  });

  it("rejects a duration outside 1 to 1440 minutes", () => {
    expect(
      validateSurgeryPlanForm(validValues({ durationMinutes: "0" })).durationMinutes,
    ).toBe("clinic.surgeries.validation.durationInvalid");
    expect(
      validateSurgeryPlanForm(validValues({ durationMinutes: "1441" })).durationMinutes,
    ).toBe("clinic.surgeries.validation.durationInvalid");
    expect(
      validateSurgeryPlanForm(validValues({ durationMinutes: "1440" })).durationMinutes,
    ).toBeUndefined();
  });
});

describe("applySurgeryType", () => {
  it("keeps existing rows, adds a row per missing default role, and keeps a typed name", () => {
    const values = validValues({
      name: "Custom name",
      team: [{ key: "row-1", userId: "user-1", role: "Surgeon" }],
    });

    const applied = applySurgeryType(values, typeFixture);

    expect(applied.catalogId).toBe(typeFixture.id);
    expect(applied.name).toBe("Custom name");
    expect(applied.tools).toEqual(["Scalpel"]);
    expect(applied.team).toHaveLength(2);
    expect(applied.team[0]).toEqual(values.team[0]);
    expect(applied.team[1]).toMatchObject({ userId: "", role: "Assistant" });
  });

  it("fills an empty name from the chosen type", () => {
    const applied = applySurgeryType(validValues({ name: "   " }), typeFixture);

    expect(applied.name).toBe("Appendectomy");
  });
});

describe("scheduledInstant", () => {
  it("round-trips with surgeryPlanFormFromSurgery in Asia/Tehran", () => {
    const values = validValues({
      scheduledDate: "2026-05-01",
      scheduledTime: "09:30",
    });
    const instant = scheduledInstant(values, "Asia/Tehran");
    expect(instant).toBeDefined();

    const surgery: Surgery = {
      id: "surgery-1",
      patientId: "patient-1",
      catalogId: typeFixture.id,
      name: values.name,
      status: "Planned",
      scheduledAt: instant!,
      team: [],
      tools: [],
      personsToNotify: [],
      contacts: [],
      reminderPolicies: [],
      concurrencyToken: "token-1",
      createdAt: instant!,
    };

    const restored = surgeryPlanFormFromSurgery(surgery, "Asia/Tehran");

    expect(restored.scheduledDate).toBe("2026-05-01");
    expect(restored.scheduledTime).toBe("09:30");
    expect(scheduledInstant(restored, "Asia/Tehran")).toBe(instant);
  });
});

describe("surgery request builders", () => {
  function storedSurgery(): Surgery {
    return {
      id: "surgery-1",
      patientId: "patient-1",
      catalogId: typeFixture.id,
      name: "Knee replacement",
      status: "Planned",
      scheduledAt: "2026-05-01T09:00:00.000Z",
      team: [],
      tools: [],
      personsToNotify: ["person-1"],
      contacts: [
        { name: "Alice", destination: "alice@example.com", channel: "Email" },
      ],
      reminderPolicies: [],
      concurrencyToken: "token-1",
      createdAt: "2026-05-01T09:00:00.000Z",
    };
  }

  it("keeps the stored contacts and persons to notify when editing", () => {
    const request = buildSurgeryWriteRequest(storedSurgery(), validValues(), {
      timeZone: "UTC",
      staff: [],
    });

    expect(request.personsToNotify).toEqual(["person-1"]);
    expect(request.contacts).toEqual([
      { name: "Alice", destination: "alice@example.com", channel: "Email" },
    ]);
  });

  it("sends empty contacts and persons to notify when creating", () => {
    const request = buildSurgeryCreateRequest(validValues(), {
      idempotencyKey: "key-1",
      timeZone: "UTC",
      staff: [],
    });

    expect(request.personsToNotify).toEqual([]);
    expect(request.contacts).toEqual([]);
  });
});

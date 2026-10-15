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
import {
  canCancelSurgery,
  canEditSurgery,
  surgeryStatuses,
  type Surgery,
  type SurgeryType,
} from "../models/surgeries/surgeryModels";

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

  it("rejects marking a surgery completed before its scheduled start", () => {
    const schedule = { timeZone: "UTC", now: new Date("2026-05-01T08:00:00Z") };

    expect(
      validateSurgeryPlanForm(validValues({ status: surgeryStatuses.completed }), schedule).status,
    ).toBe("clinic.surgeries.validation.completedInFuture");
    expect(
      validateSurgeryPlanForm(validValues({ status: surgeryStatuses.planned }), schedule).status,
    ).toBeUndefined();
    expect(
      validateSurgeryPlanForm(validValues({ status: surgeryStatuses.completed }), {
        ...schedule,
        now: new Date("2026-05-01T10:00:00Z"),
      }).status,
    ).toBeUndefined();
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

  it("sends the chosen status when editing, so a mistaken completion can be set back to planned", () => {
    const completed = { ...storedSurgery(), status: surgeryStatuses.completed };

    const request = buildSurgeryWriteRequest(completed, validValues({ status: surgeryStatuses.planned }), {
      timeZone: "UTC",
      staff: [],
    });

    expect(request.status).toBe(surgeryStatuses.planned);
  });

  it("always creates a planned surgery", () => {
    const request = buildSurgeryCreateRequest(validValues({ status: surgeryStatuses.completed }), {
      idempotencyKey: "key-1",
      timeZone: "UTC",
      staff: [],
    });

    expect(request.status).toBe(surgeryStatuses.planned);
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

describe("surgery status actions", () => {
  const surgery = (status: Surgery["status"]): Surgery => ({
    id: "surgery-1",
    patientId: "patient-1",
    catalogId: typeFixture.id,
    name: "Knee replacement",
    status,
    scheduledAt: "2026-05-01T09:00:00.000Z",
    team: [],
    tools: [],
    personsToNotify: [],
    contacts: [],
    reminderPolicies: [],
    concurrencyToken: "token-1",
    createdAt: "2026-05-01T09:00:00.000Z",
  });

  it("cancels only a planned surgery, so a completed one never sends a cancellation notice", () => {
    expect(canCancelSurgery(surgery(surgeryStatuses.planned))).toBe(true);
    expect(canCancelSurgery(surgery(surgeryStatuses.completed))).toBe(false);
    expect(canCancelSurgery(surgery(surgeryStatuses.cancelled))).toBe(false);
  });

  it("edits planned and completed surgeries but not cancelled ones", () => {
    expect(canEditSurgery(surgery(surgeryStatuses.planned))).toBe(true);
    expect(canEditSurgery(surgery(surgeryStatuses.completed))).toBe(true);
    expect(canEditSurgery(surgery(surgeryStatuses.cancelled))).toBe(false);
  });
});

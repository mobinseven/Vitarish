import type { ReactNode } from "react";
import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { SurgeryPlanForm } from "../components/surgeries/SurgeryPlanForm";
import { clinicMessageCatalogs, LocalizationProvider } from "../localization";
import { emptySurgeryPlanForm } from "../models/surgeries/surgeryPlanForm";
import type { SurgeryPlanFormValues } from "../models/surgeries/surgeryPlanForm";

const typeFixture = {
  id: "type-1",
  name: "General surgery",
  tools: ["Scalpel", "Suture"],
  defaultTeamRoles: ["Surgeon", "Assistant"],
  reminderPolicies: [
    { name: "Reminder", minutesBefore: 1440, subject: "Prep", textBody: "Prepare" },
  ],
  concurrencyToken: "catalog-token",
};

vi.mock("../hooks/surgeries/useSurgeryTypes", () => ({
  useSurgeryTypes: () => ({
    error: null,
    isLoading: false,
    refetch: vi.fn(),
    types: [typeFixture],
  }),
}));

vi.mock("../hooks/surgeries/useSurgerySuggestions", () => ({
  useSurgerySuggestions: () => ({
    data: { locations: ["Operating room 1"], roles: ["Anesthetist"] },
    isError: false,
    isPending: false,
  }),
}));

vi.mock("../hooks/surgeries/useSurgeryConflicts", () => ({
  useSurgeryConflicts: () => ({ data: [], isError: false, isPending: false }),
}));

const messages = {
  ...clinicMessageCatalogs.en,
  common: { actions: { cancel: "Cancel" } },
};

function readMessage(key: string): string {
  const value = key
    .split(".")
    .reduce<unknown>(
      (current, part) =>
        current && typeof current === "object"
          ? (current as Record<string, unknown>)[part]
          : undefined,
      messages,
    );
  return typeof value === "string" ? value : key;
}

function Wrapper({ children }: { children: ReactNode }) {
  return (
    <LocalizationProvider locale="en" messages={messages}>
      {children}
    </LocalizationProvider>
  );
}

afterEach(cleanup);

function renderForm(initialValues: SurgeryPlanFormValues, onSubmit = vi.fn()) {
  const result = render(
    <SurgeryPlanForm
      initialValues={initialValues}
      isSubmitting={false}
      mode="create"
      staff={[
        { displayName: "Ada Surgeon", userId: "user-1" },
        { displayName: "Bob Assistant", userId: "user-2" },
      ]}
      timeZone="UTC"
      onCancel={vi.fn()}
      onSubmit={onSubmit}
    />,
    { wrapper: Wrapper },
  );
  return { ...result, onSubmit };
}

describe("SurgeryPlanForm", () => {
  it("pre-fills tools, reminders, and team rows when a surgery type is chosen", async () => {
    renderForm(emptySurgeryPlanForm("patient-1"));

    fireEvent.click(
      screen.getByRole("button", {
        // The trigger's name also carries the placeholder or the selected value.
        name: (name) => name.includes(readMessage("clinic.surgeries.fields.type")),
      }),
    );
    fireEvent.click(await screen.findByRole("option", { name: "General surgery" }));

    expect(screen.getByText("Scalpel")).toBeInTheDocument();
    expect(screen.getByText("Suture")).toBeInTheDocument();
    expect(screen.getByDisplayValue("Prep")).toBeInTheDocument();
    expect(
      screen.getAllByRole("button", {
        name: (name) => name.includes(readMessage("clinic.surgeries.fields.member")),
      }),
    ).toHaveLength(typeFixture.defaultTeamRoles.length);
  });

  it("blocks saving a plan whose team repeats the same member", () => {
    const duplicated: SurgeryPlanFormValues = {
      ...emptySurgeryPlanForm("patient-1"),
      catalogId: typeFixture.id,
      name: "Knee replacement",
      scheduledDate: "2026-05-01",
      scheduledTime: "09:00",
      team: [
        { key: "row-1", role: "Surgeon", userId: "user-1" },
        { key: "row-2", role: "Assistant", userId: "user-1" },
      ],
    };
    const { container, onSubmit } = renderForm(duplicated);

    fireEvent.submit(container.querySelector("form")!);

    expect(onSubmit).not.toHaveBeenCalled();
    expect(screen.getAllByRole("alert").length).toBeGreaterThan(0);
  });
});

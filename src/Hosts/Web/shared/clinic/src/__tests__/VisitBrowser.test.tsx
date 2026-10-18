import type { ReactNode } from "react";
import userEvent from "@testing-library/user-event";
import { cleanup, render, screen, within } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { parseDate } from "@internationalized/date";
import { LocalizationProvider, clinicMessageCatalogs } from "../localization";
import type { Visit } from "../models/appointments/visitRows";

const hooks = vi.hoisted(() => ({ appointments: vi.fn() }));

vi.mock("../api/generated/clinic/clinic", () => ({
  useClinicListDashboardAppointments: hooks.appointments,
}));

import { VisitBrowser } from "../components/appointments/visits/VisitBrowser";

const patientIds = {
  sara: "12345678-1234-1234-1234-123456789012",
  reza: "22345678-1234-1234-1234-123456789012",
};

const visits: Visit[] = [
  {
    reservationId: "reservation-reza",
    patientId: patientIds.reza,
    patientDisplayName: "Reza Karimi",
    serviceId: "service-follow-up",
    serviceDisplayName: "Follow-up visit",
    date: "2026-08-27",
    startTime: "08:30:00",
    timeZone: "UTC",
    status: 0,
    patientTags: [],
  },
  {
    reservationId: "reservation-sara",
    patientId: patientIds.sara,
    patientDisplayName: "Ahmadi Sara",
    serviceId: "service-consultation",
    serviceDisplayName: "Consultation",
    date: "2026-08-27",
    startTime: "09:30:00",
    timeZone: "UTC",
    status: 1,
    patientTags: ["VIP", "Diabetes"],
  },
];

function Wrapper({ children }: { children: ReactNode }) {
  return (
    <LocalizationProvider locale="en" messages={clinicMessageCatalogs.en}>
      {children}
    </LocalizationProvider>
  );
}

function PersianWrapper({ children }: { children: ReactNode }) {
  return (
    <LocalizationProvider locale="fa" messages={clinicMessageCatalogs.fa}>
      {children}
    </LocalizationProvider>
  );
}

function answer(data: Visit[]) {
  hooks.appointments.mockReturnValue({ data, isLoading: false, error: null, refetch: vi.fn() });
}

// `hidden` also reads rows that an open popover hides from assistive technology.
const patientNames = (hidden = false) =>
  screen.getAllByRole("button", { name: /^Open .*'s profile$/, hidden }).map((button) => button.textContent);

describe("VisitBrowser", () => {
  afterEach(() => {
    cleanup();
    hooks.appointments.mockReset();
  });

  it("opens the patient from a row and a new note from its one action", async () => {
    answer(visits);
    const onNavigate = vi.fn();
    const user = userEvent.setup();
    render(<VisitBrowser onNavigate={onNavigate} />, { wrapper: Wrapper });

    expect(patientNames()).toEqual(["Reza Karimi", "Ahmadi Sara"]);
    const tags = screen.getByRole("list", { name: "Patient tags" });
    expect(within(tags).getAllByRole("listitem").map((item) => item.textContent)).toEqual(["VIP", "Diabetes"]);

    await user.click(screen.getByRole("button", { name: "Open Ahmadi Sara's profile" }));
    expect(onNavigate).toHaveBeenLastCalledWith({ kind: "patientDetail", patientId: patientIds.sara, quickAction: undefined });

    await user.click(screen.getByRole("button", { name: "New note for Ahmadi Sara" }));
    expect(onNavigate).toHaveBeenLastCalledWith({ kind: "patientDetail", patientId: patientIds.sara, quickAction: "newVisit" });
  });

  it("queries the whole week or month and steps by the chosen period", async () => {
    answer([]);
    const user = userEvent.setup();
    render(<VisitBrowser onNavigate={vi.fn()} />, { wrapper: Wrapper });

    const today = hooks.appointments.mock.calls.at(-1)?.[0];
    expect(today.dateFrom).toBe(today.dateTo);
    expect(screen.getByText("No visits on this day")).toBeVisible();

    await user.click(screen.getByRole("button", { name: "Previous day" }));
    expect(hooks.appointments.mock.calls.at(-1)?.[0].dateFrom).toBe(
      parseDate(today.dateFrom).subtract({ days: 1 }).toString(),
    );

    await user.click(screen.getByRole("radio", { name: "Week" }));
    const week = hooks.appointments.mock.calls.at(-1)?.[0];
    expect(parseDate(week.dateTo).compare(parseDate(week.dateFrom))).toBe(6);
    expect(screen.getByText("No visits in this period")).toBeVisible();

    await user.click(screen.getByRole("button", { name: "Next week" }));
    expect(hooks.appointments.mock.calls.at(-1)?.[0].dateFrom).toBe(
      parseDate(week.dateFrom).add({ weeks: 1 }).toString(),
    );
  });

  it("searches, filters by status, and sorts by patient", async () => {
    answer(visits);
    const user = userEvent.setup();
    render(<VisitBrowser onNavigate={vi.fn()} />, { wrapper: Wrapper });

    await user.click(screen.getByRole("button", { name: "Sort by time" }));
    expect(patientNames()).toEqual(["Ahmadi Sara", "Reza Karimi"]);

    await user.type(screen.getByRole("textbox", { name: "Search visits" }), "reza");
    expect(patientNames()).toEqual(["Reza Karimi"]);
    await user.clear(screen.getByRole("textbox", { name: "Search visits" }));

    await user.click(screen.getByRole("button", { name: "Filters" }));
    await user.click(screen.getByRole("button", { name: /Status/ }));
    await user.click(screen.getByRole("option", { name: "Confirmed" }));
    await user.keyboard("{Escape}");
    expect(screen.getByText("1 visits")).toBeInTheDocument();
    expect(patientNames(true)).toEqual(["Ahmadi Sara"]);

    await user.click(screen.getByRole("button", { name: "Clear all filters", hidden: true }));
    expect(patientNames(true)).toEqual(["Ahmadi Sara", "Reza Karimi"]);
  });

  it("points the period steps along the reading direction in RTL", () => {
    answer([]);
    render(<VisitBrowser onNavigate={vi.fn()} />, { wrapper: PersianWrapper });
    const visitsFa = clinicMessageCatalogs.fa.clinic.appointments.visits;

    expect(screen.getByRole("button", { name: visitsFa.previous.day }).querySelector("i")).toHaveClass("fa-chevron-right");
    expect(screen.getByRole("button", { name: visitsFa.next.day }).querySelector("i")).toHaveClass("fa-chevron-left");
  });
});

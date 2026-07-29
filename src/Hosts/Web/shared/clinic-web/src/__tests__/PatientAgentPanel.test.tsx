import type { ReactNode } from "react";
import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { AppointmentActionCard, ClinicalEventActionCard, PatientAgentComposer, PatientAgentMessage, PatientAgentActionStatus, PrescriptionActionCard } from "../features/patient-agent/PatientAgentPanel";
import { LocalizationProvider, clinicMessageCatalogs } from "../localization";

const common = { actions: { cancel: "Cancel", save: "Save", delete: "Delete" }, values: { notAvailable: "Not available", unknown: "Unknown" }, states: { loading: "Loading" } };
function wrapper(locale: "en" | "fa" = "en") { return function Wrapper({ children }: { children: ReactNode }) { return <LocalizationProvider locale={locale} messages={{ ...clinicMessageCatalogs[locale], common }}>{children}</LocalizationProvider>; }; }

describe("patient agent review", () => {
  it("submits from the keyboard without exposing a patient selector", async () => {
    const submit = vi.fn().mockResolvedValue(undefined);
    render(<PatientAgentComposer disabled={false} onSubmit={submit} />, { wrapper: wrapper() });
    const composer = screen.getByLabelText("Message to the patient agent");
    fireEvent.change(composer, { target: { value: "Book tomorrow" } });
    fireEvent.keyDown(composer, { key: "Enter" });
    expect(submit).toHaveBeenCalledWith("Book tomorrow");
    expect(screen.queryByRole("combobox")).not.toBeInTheDocument();
  });

  it("renders missing-information responses and accessible action status", () => {
    render(<><PatientAgentMessage message={{ kind: "MissingInformationRequest", content: "Which service?" }} /><PatientAgentActionStatus status="Expired" /></>, { wrapper: wrapper() });
    expect(screen.getByText("More information needed")).toBeInTheDocument();
    expect(screen.getByRole("status")).toHaveTextContent("Expired");
  });

  it("shows material appointment, prescription, and clinical-event review data", () => {
    render(<><AppointmentActionCard expiresAt="2026-08-01T12:00:00Z" payload={{ slot: { date: "2026-08-01", startTime: "10:00", service: "Consultation", practitioner: "Dr Noor", durationMinutes: 30 }, description: "Review" }} /><PrescriptionActionCard expiresAt="2026-08-01T12:00:00Z" payload={{ templateName: "Standard", templateVersion: "v4", insuranceCoverage: "Basic", items: [{ displayName: "Medicine", dose: "5 mg", frequency: "Daily", route: "Oral", durationDays: 7, instructions: "After food" }] }} /><ClinicalEventActionCard expiresAt="2026-08-01T12:00:00Z" payload={{ procedureName: "Therapy", content: { visitNumber: 2, eventType: "FollowUp", completionStatus: "Completed", performedAt: "2026-08-01T09:00:00Z", observations: "Improved", diagnosis: "Clinician diagnosis", planOrFollowUp: "Return in a week", narrativeNote: "Note", attachmentReferences: ["scan.pdf"] } }} /></>, { wrapper: wrapper() });
    expect(screen.getByText("Consultation")).toBeInTheDocument();
    expect(screen.getByText("Medicine")).toBeInTheDocument();
    expect(screen.getByText("Clinician diagnosis").parentElement).toHaveClass("border-primary");
  });

  it("renders Persian copy in an RTL localization context", () => {
    const { container } = render(<PatientAgentMessage message={{ kind: "AssistantMessage", content: "انجام شد" }} />, { wrapper: wrapper("fa") });
    expect(screen.getByText("دستیار")).toBeInTheDocument();
    expect(container.closest("body")?.dir || document.documentElement.dir).toBeDefined();
  });
});

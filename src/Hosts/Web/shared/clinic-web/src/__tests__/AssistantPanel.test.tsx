import type { ReactNode } from "react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import {
  AppointmentActionCard,
  ClinicalEventActionCard,
  PrescriptionActionCard,
} from "../features/clinic-assistant/ActionCards";
import { AssistantActionStatus } from "../features/clinic-assistant/AssistantActions";
import {
  AssistantComposer,
  AssistantMessage,
  AssistantPanel,
} from "../features/clinic-assistant/AssistantPanel";
import { LocalizationProvider, clinicMessageCatalogs } from "../localization";

vi.mock("@vitarish/web-ui", () => ({
  FaIcon: ({ icon }: { icon: string }) => (
    <span data-icon={icon} aria-hidden="true" />
  ),
}));
vi.mock("@vitarish/intelligence-web", async () => {
  const actual = await vi.importActual<any>("@vitarish/intelligence-web");
  return {
    ...actual,
    AgentConversationComposer: ({ value, onChange, onSubmit, label, placeholder }: any) => (
      <>
        <textarea
          aria-label={label}
          placeholder={placeholder}
          value={value}
          onChange={(event) => onChange(event.target.value)}
          onKeyDown={(event) => {
            if (event.key === "Enter" && !event.shiftKey) {
              event.preventDefault();
              void onSubmit(value);
            }
          }}
        />
        <button aria-label="Dictate draft" onClick={() => onChange(`${value} dictated`)}>Dictate</button>
      </>
    ),
  };
});

afterEach(cleanup);

const common = {
  actions: { cancel: "Cancel", save: "Save", delete: "Delete" },
  values: { notAvailable: "Not available", unknown: "Unknown" },
  states: { loading: "Loading" },
};
function wrapper(locale: "en" | "fa" = "en") {
  return function Wrapper({ children }: { children: ReactNode }) {
    return (
      <LocalizationProvider
        locale={locale}
        messages={{ ...clinicMessageCatalogs[locale], common }}
      >
        {children}
      </LocalizationProvider>
    );
  };
}
function panelWrapper(locale: "en" | "fa") {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return function Wrapper({ children }: { children: ReactNode }) {
    return (
      <QueryClientProvider client={client}>
        <LocalizationProvider
          locale={locale}
          messages={{ ...clinicMessageCatalogs[locale], common }}
        >
          {children}
        </LocalizationProvider>
      </QueryClientProvider>
    );
  };
}

describe("clinic assistant review", () => {
  it("submits from the keyboard without exposing a patient selector", async () => {
    const submit = vi.fn().mockResolvedValue(undefined);
    render(<AssistantComposer disabled={false} onSubmit={submit} />, {
      wrapper: wrapper(),
    });
    const composer = screen.getByLabelText("Message to the clinic assistant");
    fireEvent.change(composer, { target: { value: "Book tomorrow" } });
    fireEvent.keyDown(composer, { key: "Enter" });
    expect(submit).toHaveBeenCalledWith("Book tomorrow");
    expect(screen.queryByRole("combobox")).not.toBeInTheDocument();
  });

  it("submits a dictated draft without adding a patient selector", async () => {
    const submit = vi.fn().mockResolvedValue(undefined);
    render(<AssistantComposer disabled={false} onSubmit={submit} />, {
      wrapper: wrapper(),
    });
    fireEvent.click(screen.getByRole("button", { name: "Dictate draft" }));
    const composer = screen.getByLabelText("Message to the clinic assistant");
    fireEvent.keyDown(composer, { key: "Enter" });
    expect(submit).toHaveBeenCalledWith("dictated");
    expect(screen.queryByRole("combobox")).not.toBeInTheDocument();
  });

  it("renders missing-information responses and accessible action status", () => {
    render(
      <>
        <AssistantMessage
          message={{
            kind: "MissingInformationRequest",
            content: "Which service?",
          }}
        />
        <AssistantActionStatus status="Expired" />
      </>,
      { wrapper: wrapper() },
    );
    expect(screen.getByText("More information needed")).toBeInTheDocument();
    expect(screen.getByRole("status")).toHaveTextContent("Expired");
  });

  it("renders agent failures as an error message with a retry action", () => {
    const retry = vi.fn();
    render(
      <AssistantMessage
        message={{ kind: "Failure", content: "provider detail" }}
        onRetry={retry}
      />,
      { wrapper: wrapper() },
    );

    expect(screen.getByRole("alert")).toHaveTextContent("The clinic assistant request could not be completed.");
    expect(screen.queryByText("provider detail")).not.toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Retry request" }));
    expect(retry).toHaveBeenCalledOnce();
  });

  it("shows material appointment, prescription, and clinical-event review data", () => {
    render(
      <>
        <AppointmentActionCard
          expiresAt="2026-08-01T12:00:00Z"
          payload={{
            slot: {
              date: "2026-08-01",
              startTime: "10:00",
              service: "Consultation",
              practitioner: "Dr Noor",
              durationMinutes: 30,
            },
            description: "Review",
          }}
        />
        <PrescriptionActionCard
          expiresAt="2026-08-01T12:00:00Z"
          payload={{
            templateName: "Standard",
            templateVersion: "v4",
            insuranceCoverage: "Basic",
            items: [
              {
                displayName: "Medicine",
                dose: "5 mg",
                frequency: "Daily",
                route: "Oral",
                durationDays: 7,
                instructions: "After food",
              },
            ],
          }}
        />
        <ClinicalEventActionCard
          expiresAt="2026-08-01T12:00:00Z"
          payload={{
            procedureName: "Therapy",
            content: {
              visitNumber: 2,
              eventType: "FollowUp",
              completionStatus: "Completed",
              performedAt: "2026-08-01T09:00:00Z",
              observations: "Improved",
              diagnosis: "Clinician diagnosis",
              planOrFollowUp: "Return in a week",
              narrativeNote: "Note",
              attachmentReferences: ["scan.pdf"],
            },
          }}
        />
      </>,
      { wrapper: wrapper() },
    );
    expect(screen.getByText("Consultation")).toBeInTheDocument();
    expect(screen.getByText("Medicine")).toBeInTheDocument();
    expect(screen.getByText("Clinician diagnosis").parentElement).toHaveClass(
      "border-primary",
    );
  });

  it("renders Persian copy in an RTL localization context", () => {
    render(
      <AssistantPanel
        patientId="patient-1"
        patientName="مریم احمدی"
        isOpen
        onOpenChange={() => undefined}
      />,
      { wrapper: panelWrapper("fa") },
    );
    const panel = screen.getByRole("region", {
      name: "دستیار فضای کاری بیمار",
    });
    expect(panel).toHaveAttribute("dir", "rtl");
    expect(screen.getByText("در حال کار برای مریم احمدی")).toBeInTheDocument();
    expect(screen.queryByRole("combobox")).not.toBeInTheDocument();
  });
});

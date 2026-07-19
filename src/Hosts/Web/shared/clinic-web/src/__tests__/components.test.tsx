import type { ReactNode } from "react";
import { cleanup, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { LocalizationProvider } from "../localization";
import { clinicMessageCatalogs } from "../localization";
import { ServiceList } from "../components/services/ServiceList";
import { ServiceEditor } from "../components/services/ServiceEditor";
import { ServiceTypeEditor } from "../components/service-types/ServiceTypeEditor";
import { ServiceProviderEditor } from "../components/service-providers/ServiceProviderEditor";
import { ShiftEditor } from "../components/appointments/shifts/ShiftEditor";
import { ServiceDeleteDialog } from "../components/services/ServiceDeleteDialog";
import { PatientProfileForm } from "../components/patients/profile/PatientProfileForm";
import { emptyFormData } from "../models/patients/profile/PatientProfileForm";
import { PatientProcedureEventTimeline } from "../components/patients/procedures/PatientProcedureEventTimeline";
import { AppointmentsAgenda } from "../components/appointments/agenda/AppointmentsAgenda";
import {
  PatientProcedureEventItemsPanel,
} from "../components/patients/procedures/PatientProcedureEventItemsPanel";
import { PatientProcedureEventItemMessage } from "../components/patients/procedures/PatientProcedureEventItemMessage";
import { PatientSearchBar } from "../components/patients/list/PatientSearchBar";
import { PatientsGrid } from "../components/patients/list/PatientsGrid";

vi.mock("../hooks/patients/procedures/usePatientProcedureEventItems", () => ({
  usePatientProcedureEventItems: () => ({
    data: [],
    error: null,
    isLoading: false,
    refetch: vi.fn(),
  }),
}));

vi.mock("../hooks/patients/procedures/usePatientProcedureEventItemMutations", () => ({
  useCreatePatientProcedureEventItemFile: () => ({
    isError: false,
    isPending: false,
    mutateAsync: vi.fn(),
    reset: vi.fn(),
  }),
  useCreatePatientProcedureEventItemText: () => ({
    isError: false,
    isPending: false,
    mutateAsync: vi.fn(),
  }),
  useDeletePatientProcedureEventItem: () => ({
    isError: false,
    isPending: false,
    mutate: vi.fn(),
  }),
}));

vi.mock("@vitarish/intelligence-web", () => ({
  appendConfirmedTranscript: (current: string, segment: string) =>
    current ? `${current} ${segment}` : segment,
  LiveDictation: () => null,
  useLiveDictation: () => ({
    available: false,
    capabilitiesLoading: false,
    failureCode: "CapabilityUnavailable",
    isActive: false,
    language: "en-US",
    languages: [],
    setLanguage: vi.fn(),
    start: vi.fn(),
    status: "unavailable",
    stop: vi.fn(),
  }),
}));

const messages = {
  ...clinicMessageCatalogs.en,
  common: {
    actions: { cancel: "Cancel", save: "Save", delete: "Delete" },
    values: { notAvailable: "Not available", unknown: "Unknown" },
    states: { loading: "Loading" },
  },
};

afterEach(() => {
  cleanup();
  vi.unstubAllGlobals();
});

function Wrapper({ children }: { children: ReactNode }) {
  return <LocalizationProvider locale="en" messages={messages}>{children}</LocalizationProvider>;
}

function PersianWrapper({ children }: { children: ReactNode }) {
  return <LocalizationProvider locale="fa" messages={clinicMessageCatalogs.fa}>{children}</LocalizationProvider>;
}

describe("clinic display components", () => {
  it("uses the HeroUI search field for debounced patient searches", async () => {
    const onSearch = vi.fn();
    const { container } = render(<PatientSearchBar onSearch={onSearch} />, { wrapper: Wrapper });

    expect(container.querySelector('[data-slot="search-field"]')).toBeInTheDocument();
    fireEvent.change(screen.getByRole("searchbox", { name: "Search patients" }), {
      target: { value: "Ada" },
    });

    await waitFor(() => expect(onSearch).toHaveBeenCalledWith("Ada"));
  });

  it("renders a HeroUI empty state when no patients match the search", () => {
    const { container } = render(
      <PatientsGrid
        patients={[]}
        totalCount={0}
        descending={false}
        onAddPatient={vi.fn()}
        onSearch={vi.fn()}
        onSelectPatient={vi.fn()}
        onSort={vi.fn()}
      />,
      { wrapper: Wrapper },
    );

    expect(container.querySelector('[data-slot="empty-state"]')).toBeInTheDocument();
    expect(screen.getByRole("heading", { name: "No patients found" })).toBeInTheDocument();
    expect(screen.getByText("Try adjusting your search.")).toBeInTheDocument();
  });

  it("renders responsive patient cards with an icon avatar, selection, and sorting", () => {
    const onSelectPatient = vi.fn();
    const onSort = vi.fn();
    const { container } = render(
      <PatientsGrid
        patients={[
          {
            id: "patient-1",
            firstName: "Ada Ada Ada Ada Ada Ada Ada Ada Ada Ada Ada Ada Ada Ada Ada",
            lastName: "Lovelace",
            identificationCode: "P-100",
            phone: "+98 912 000 0000",
          },
        ]}
        totalCount={1}
        descending={false}
        onAddPatient={vi.fn()}
        onSearch={vi.fn()}
        onSelectPatient={onSelectPatient}
        onSort={onSort}
      />,
      { wrapper: Wrapper },
    );

    const cards = container.querySelector(".sm\\:grid-cols-2");
    expect(cards).toHaveClass("xl:grid-cols-3");
    expect(screen.getByRole("toolbar", { name: "Patients" })).toHaveClass(
      "grid-flow-row",
      "grid-cols-1",
    );
    expect(screen.getByRole("toolbar", { name: "Patients" })).toHaveClass("toolbar--attached");
    expect(container.querySelector('[data-slot="scroll-shadow"]')).not.toHaveClass("overflow-y-auto");
    const searchField = screen.getByRole("searchbox", { name: "Search patients" }).closest(
      '[data-slot="search-field"]',
    );
    expect(searchField?.parentElement).toHaveClass(
      "order-2",
      "md:flex-1",
    );
    expect(screen.getByLabelText("1 patient found")).toHaveClass(
      "order-3",
      "justify-self-start",
    );
    expect(screen.getByRole("button", { name: "Sort" }).parentElement).toHaveClass(
      "order-1",
      "justify-self-center",
    );
    expect(screen.getByRole("button", { name: "New Patient" })).toHaveClass(
      "order-4",
      "w-full",
    );
    expect(container.querySelector('[data-slot="item-card"]')).toHaveClass("item-card--secondary");
    expect(container.querySelector(".fa-user")).toBeInTheDocument();
    expect(screen.getByText(/Ada Ada Ada/)).toBeInTheDocument();
    expect(screen.getByText(/Identification Code: P-100 \/ \+98 912 000 0000/)).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: /Ada Ada Ada/ }));
    expect(onSelectPatient).toHaveBeenCalledWith("patient-1");

    fireEvent.click(screen.getByRole("button", { name: "Sort" }));
    fireEvent.click(screen.getByRole("menuitem", { name: "Identification Code" }));
    expect(onSort).toHaveBeenCalledWith("IdentificationCode");
  });

  it("lets the conversation own its bounded scrollport while keeping the composer outside it", () => {
    const { container } = render(
      <PatientProcedureEventItemsPanel canWrite eventId="event-1" />,
      { wrapper: Wrapper },
    );

    const conversation = screen.getByRole("log");
    const composer = screen.getByLabelText("Clinical note");

    expect(conversation).toHaveClass("min-h-0", "flex-1");
    expect(conversation.closest('[data-slot="scroll-shadow"]')).toBeNull();
    expect(conversation).not.toContainElement(composer);
  });

  it("uses a unique heading relationship for each event items panel", () => {
    const { container } = render(
      <>
        <PatientProcedureEventItemsPanel canWrite={false} eventId="event-1" />
        <PatientProcedureEventItemsPanel canWrite={false} eventId="event-2" />
      </>,
      { wrapper: Wrapper },
    );
    const panels = [...container.querySelectorAll("section[aria-labelledby]")];
    const headingIds = panels.map((panel) => panel.getAttribute("aria-labelledby"));

    expect(panels).toHaveLength(2);
    expect(new Set(headingIds).size).toBe(2);
    for (const headingId of headingIds) {
      expect(headingId && container.ownerDocument.getElementById(headingId)).not.toBeNull();
    }
  });

  it("renders agenda weekday names from the selected culture instead of the browser culture", () => {
    const { container } = render(<AppointmentsAgenda shifts={[]} reservations={[]} />, { wrapper: PersianWrapper });
    const weekdayNames = [...container.querySelectorAll('[data-slot="agenda-day-header-name"]')];

    expect(weekdayNames).toHaveLength(7);
    for (const weekdayName of weekdayNames) {
      expect(weekdayName).toHaveTextContent(/[\u0600-\u06ff]/);
    }

    expect(screen.getByRole("radio", { name: "روز" })).toBeInTheDocument();
    expect(screen.getByRole("radio", { name: "هفته" })).toBeInTheDocument();
    expect(screen.getByRole("radio", { name: "ماه" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "بازه قبلی" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "امروز" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "بازه بعدی" })).toBeInTheDocument();
  });

  it("switches from the weekly time grid to the month grid", () => {
    const { container } = render(<AppointmentsAgenda shifts={[]} reservations={[]} />, { wrapper: Wrapper });

    expect(container.querySelector('[data-slot="agenda-time-grid"]')).toBeInTheDocument();
    expect(container.querySelector('[data-slot="agenda-month-grid"]')).not.toBeInTheDocument();

    fireEvent.click(screen.getByRole("radio", { name: "Month" }));

    expect(container.querySelector('[data-slot="agenda-time-grid"]')).not.toBeInTheDocument();
    expect(container.querySelector('[data-slot="agenda-month-grid"]')).toBeInTheDocument();
    const monthRows = [...container.querySelectorAll('[data-slot="agenda-month-row"]')];
    expect(monthRows.length).toBeGreaterThanOrEqual(4);
    for (const row of monthRows) {
      expect(row.querySelectorAll('[data-slot="agenda-month-cell"]')).toHaveLength(7);
    }
  });

  it("opens image attachments in a focused preview without technical details in the chat", async () => {
    const mediaItem = {
      id: "media-1",
      eventId: "event-1",
      type: "media" as const,
      content: "data:image/png;base64,original",
      contentThumbnail: "data:image/png;base64,thumbnail",
      contentType: "image/png",
      fileName: "scan.png",
      size: 1024,
      authorId: "clinician-2",
      authorName: "Grace Hopper",
      createdAt: "2026-07-14T08:00:00Z",
    };
    render(
      <PatientProcedureEventItemMessage
        canWrite={false}
        isDeleting={false}
        item={mediaItem}
        viewer={{ id: "clinician-1", label: "Ava Patel" }}
        onDelete={vi.fn()}
      />,
      { wrapper: Wrapper },
    );

    expect(screen.getByRole("img", { name: "scan.png" })).toHaveAttribute(
      "src",
      mediaItem.contentThumbnail,
    );
    expect(screen.queryByText("image/png")).not.toBeInTheDocument();
    expect(screen.queryByText("1 KB")).not.toBeInTheDocument();
    expect(screen.getByRole("article")).toHaveClass("chat-message--assistant");
    expect(screen.getByText("Grace Hopper")).toHaveAttribute("data-slot", "avatar-fallback");
    expect(document.querySelector(".chat-message__avatar")).not.toBeNull();

    fireEvent.contextMenu(screen.getByRole("article"));
    expect(await screen.findByRole("menuitem", { name: /Grace Hopper/ })).toBeInTheDocument();
    fireEvent.keyDown(screen.getByRole("menu"), { key: "Escape" });
    await waitFor(() => expect(screen.queryByRole("menu")).not.toBeInTheDocument());

    fireEvent.click(screen.getByRole("button", { name: "Preview scan.png" }));

    const dialog = screen.getByRole("dialog", { name: "scan.png" });
    expect(within(dialog).getByRole("img", { name: "scan.png" })).toHaveAttribute(
      "src",
      mediaItem.content,
    );
    expect(within(dialog).getByText("image/png")).toBeInTheDocument();
    expect(within(dialog).getByText("1 kB")).toBeInTheDocument();
    expect(within(dialog).getByRole("link", { name: "Download" }))
      .toHaveAttribute("download", "scan.png");
  });

  it("resolves legacy image IDs without displaying the identifier", async () => {
    const legacyId = "019ea70e-dc73-73b3-9f03-1744bc58eeee";
    const fetchMock = vi
      .fn()
      .mockResolvedValue(new Response(new Blob(["image"], { type: "image/png" })));
    vi.stubGlobal("fetch", fetchMock);

    render(
      <PatientProcedureEventItemMessage
        canWrite={false}
        isDeleting={false}
        item={{
          id: "legacy-media-1",
          eventId: "event-1",
          type: "media",
          content: legacyId,
          contentType: "image/png",
          fileName: "legacy-scan.png",
          size: 5,
          authorId: "clinician-2",
          createdAt: "2026-07-14T08:00:00Z",
        }}
        viewer={{ id: "clinician-1", label: "Ava Patel" }}
        onDelete={vi.fn()}
      />,
      { wrapper: Wrapper },
    );

    expect(screen.queryByText(legacyId)).not.toBeInTheDocument();
    await waitFor(() =>
      expect(screen.getByRole("button", { name: "Preview legacy-scan.png" })).toBeEnabled(),
    );
    expect(screen.getByRole("img", { name: "legacy-scan.png" })).toHaveAttribute(
      "src",
      "blob:vitest",
    );
    expect(String(fetchMock.mock.calls[0][0])).toContain(
      `/api/modules/files/${legacyId}/content`,
    );
  });

  it("keeps file downloads and destructive actions behind familiar controls", async () => {
    const onDelete = vi.fn();
    const fileItem = {
      id: "file-1",
      eventId: "event-1",
      type: "file" as const,
      content: "data:application/pdf;base64,document",
      contentType: "application/pdf",
      fileName: "instructions.pdf",
      size: 2048,
      authorId: "clinician-1",
      createdAt: "2026-07-14T09:00:00Z",
    };

    render(
      <PatientProcedureEventItemMessage
        canWrite
        isDeleting={false}
        item={fileItem}
        viewer={{ id: "clinician-1", label: "Ava Patel" }}
        onDelete={onDelete}
      />,
      { wrapper: Wrapper },
    );

    const downloadLink = screen.getByRole("link", { name: "Download instructions.pdf" });
    expect(downloadLink).toHaveAttribute("download", "instructions.pdf");
    expect(downloadLink).toHaveAttribute("href", fileItem.content);
    expect(screen.queryByText("application/pdf")).not.toBeInTheDocument();
    expect(screen.queryByRole("button", { name: "Delete Item" })).not.toBeInTheDocument();
    expect(screen.getByRole("article")).toHaveClass("chat-message--user");
    expect(screen.queryByText("Ava Patel")).not.toBeInTheDocument();
    expect(screen.queryByRole("time")).not.toBeInTheDocument();

    fireEvent.contextMenu(screen.getByRole("article"));
    const deleteMenuItem = await screen.findByRole("menuitem", { name: "Delete Item" });
    fireEvent.click(deleteMenuItem);
    expect(onDelete).toHaveBeenCalledOnce();
  });

  it("focuses a clinical timeline entry before opening its details", () => {
    const onNavigate = vi.fn();
    const onSelectEvent = vi.fn();
    render(<PatientProcedureEventTimeline patientId="patient-1" procedureId="procedure-1" selectedEventId="event-2" events={[
      { id: "event-1", projectId: "patient-1", procedureId: "procedure-1", title: "Assessment", description: "", isCommitted: true, committedAt: "2026-07-13T08:00:00Z", itemCount: 2, createdAt: "2026-07-13T07:00:00Z", position: 0 },
      { id: "event-2", projectId: "patient-1", procedureId: "procedure-1", title: "Treatment", description: "", isCommitted: false, itemCount: 0, createdAt: "2026-07-13T09:00:00Z", position: 1 },
    ]} selectedEventItems={[
      { id: "file-1", eventId: "event-2", type: "file", content: "file-1", contentType: "application/pdf", fileName: "assessment.pdf", size: 2048, createdAt: "2026-07-13T09:01:00Z" },
      { id: "file-2", eventId: "event-2", type: "file", content: "file-2", fileName: "plan.txt", createdAt: "2026-07-13T09:02:00Z" },
      { id: "file-3", eventId: "event-2", type: "file", content: "file-3", fileName: "scan.dcm", createdAt: "2026-07-13T09:03:00Z" },
      { id: "file-4", eventId: "event-2", type: "file", content: "file-4", fileName: "notes.docx", createdAt: "2026-07-13T09:04:00Z" },
    ]} onNavigate={onNavigate} onSelectEvent={onSelectEvent} />, { wrapper: Wrapper });
    fireEvent.click(screen.getByRole("button", { name: "Treatment" }));
    expect(onSelectEvent).toHaveBeenCalledWith("event-2");
    expect(onNavigate).not.toHaveBeenCalled();
    expect(screen.getByRole("region", { name: "Focused event details" })).toHaveTextContent("Treatment");
    expect(screen.getByRole("group", { name: "Event attachments" })).toHaveTextContent("assessment.pdf");
    expect(screen.getByText("+1 more")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Open details" }));
    expect(onNavigate).toHaveBeenCalledWith({ kind: "patientProcedureEventDetail", patientId: "patient-1", procedureId: "procedure-1", eventId: "event-2" });
    expect(screen.queryByRole("button", { name: "Move Step Earlier" })).not.toBeInTheDocument();
  });

  it("renders service presentation models and delegates delete", () => {
    const onDelete = vi.fn();
    render(<ServiceList items={[{ id: "s-1", name: "Consultation", serviceTypeName: "Visit" }]} onDelete={onDelete} />, { wrapper: Wrapper });
    expect(screen.getByText("Consultation")).toBeInTheDocument();
    expect(screen.getByText("Visit")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "Delete" }));
    expect(onDelete).toHaveBeenCalledWith("s-1");
  });
});

describe("clinic editors", () => {
  it("renders patient date pickers using the selected culture", () => {
    const initialData = {
      ...emptyFormData(),
      dateOfBirth: "2026-07-15",
      dateOfSurgery: "2026-07-16",
    };
    const { container } = render(
      <PatientProfileForm initialData={initialData} onSubmit={vi.fn()} onCancel={vi.fn()} />,
      { wrapper: PersianWrapper },
    );
    const datePickers = [...container.querySelectorAll('[data-slot="date-picker"]')];

    expect(datePickers).toHaveLength(2);
    expect(screen.getByText("تاریخ تولد")).toBeInTheDocument();
    expect(screen.getByText("تاریخ جراحی")).toBeInTheDocument();
    for (const datePicker of datePickers) {
      expect(datePicker).toHaveTextContent(/[۰-۹]/);
    }
  });

  it("validates and submits the patient editor", () => {
    const onSubmit = vi.fn();
    render(<PatientProfileForm initialData={emptyFormData()} onSubmit={onSubmit} onCancel={vi.fn()} />, { wrapper: Wrapper });
    const form = screen.getByRole("button", { name: "Save Patient" }).closest("form");
    expect(form).not.toBeNull();
    fireEvent.submit(form!);
    expect(screen.getByText("First name is required.")).toBeInTheDocument();
    expect(screen.getByText("Last name is required.")).toBeInTheDocument();
    fireEvent.change(screen.getByLabelText("First Name"), { target: { value: "Ada" } });
    fireEvent.change(screen.getByLabelText("Last Name"), { target: { value: "Lovelace" } });
    fireEvent.submit(form!);
    expect(onSubmit).toHaveBeenCalledWith(expect.objectContaining({ firstName: "Ada", lastName: "Lovelace" }));
  });

  it.each([
    ["service", ServiceEditor],
    ["service type", ServiceTypeEditor],
    ["service provider", ServiceProviderEditor],
  ] as const)("submits localized %s editor values", (_name, Editor) => {
    const onSubmit = vi.fn();
    render(<Editor initialValue={{ name: "Initial", description: "Description" }} isPending={false} onSubmit={onSubmit} onCancel={vi.fn()} />, { wrapper: Wrapper });
    fireEvent.change(screen.getByLabelText("Name"), { target: { value: "Updated" } });
    fireEvent.click(screen.getByRole("button", { name: "Save" }));
    expect(onSubmit).toHaveBeenCalledWith({ name: "Updated", description: "Description" });
  });

  it("submits shift values through the shift editor", () => {
    const onSubmit = vi.fn();
    render(<ShiftEditor initialValue={{ dayOfWeek: 1, startTime: "09:00", endTime: "10:00", totalSeats: 10, timeZone: "UTC" }} isPending={false} onSubmit={onSubmit} onCancel={vi.fn()} />, { wrapper: Wrapper });
    fireEvent.click(screen.getByRole("button", { name: "Save" }));
    expect(onSubmit).toHaveBeenCalledWith(expect.objectContaining({ dayOfWeek: 1, startTime: "09:00", endTime: "10:00", totalSeats: 10, timeZone: "UTC" }));
  });
});

describe("clinic delete dialogs", () => {
  it("exposes cancel and confirm actions", () => {
    const onCancel = vi.fn();
    const onConfirm = vi.fn();
    render(<ServiceDeleteDialog isOpen isPending={false} onCancel={onCancel} onConfirm={onConfirm} />, { wrapper: Wrapper });
    expect(screen.getByRole("dialog")).toHaveAccessibleName("Delete Service");
    fireEvent.click(screen.getByRole("button", { name: "Delete" }));
    expect(onConfirm).toHaveBeenCalledOnce();
    fireEvent.click(screen.getByRole("button", { name: "Cancel" }));
    expect(onCancel).toHaveBeenCalledOnce();
  });

  it("disables cancellation while deletion is pending", () => {
    render(<ServiceDeleteDialog isOpen isPending onCancel={vi.fn()} onConfirm={vi.fn()} />, { wrapper: Wrapper });
    expect(screen.getByRole("button", { name: "Cancel" })).toBeDisabled();
  });
});

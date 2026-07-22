import type { ReactNode } from "react";
import {
  act,
  cleanup,
  fireEvent,
  render,
  screen,
} from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { PatientProcedureEventItemsPanel } from "../components/patients/procedures/PatientProcedureEventItemsPanel";
import { clinicMessageCatalogs, LocalizationProvider } from "../localization";

const dictation = vi.hoisted(() => ({
  options: null as null | {
    enabled: boolean;
    onSegment: (text: string) => void;
    sessionKey: string;
  },
  sessionKeys: [] as string[],
  start: vi.fn(async () => undefined),
  stop: vi.fn(async () => undefined),
}));

vi.mock("../hooks/patients/procedures/usePatientProcedureEventItems", () => ({
  usePatientProcedureEventItems: () => ({
    data: [],
    error: null,
    isLoading: false,
    refetch: vi.fn(),
  }),
}));

vi.mock(
  "../hooks/patients/procedures/usePatientProcedureEventItemMutations",
  () => ({
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
  }),
);

vi.mock("@vitarish/intelligence-web", () => ({
  appendConfirmedTranscript: (current: string, segment: string) =>
    current ? `${current} ${segment.trim()}` : segment.trim(),
  LiveDictation: ({
    controller,
  }: {
    controller: { status: string; stop: () => void };
  }) => (
    <>
      <button
        aria-label="Stop voice dictation"
        onClick={() => controller.stop()}
      />
      <p role="status">Recording. Select stop when you are finished.</p>
    </>
  ),
  useLiveDictation: (options: {
    enabled: boolean;
    onSegment: (text: string) => void;
    sessionKey: string;
  }) => {
    dictation.options = options;
    dictation.sessionKeys.push(options.sessionKey);
    return {
      available: true,
      capabilitiesLoading: false,
      failureCode: null,
      isActive: true,
      language: "en-US",
      languages: ["fa-IR", "en-US"],
      setLanguage: vi.fn(),
      start: dictation.start,
      status: "recording",
      stop: dictation.stop,
    };
  },
}));

const messages = {
  ...clinicMessageCatalogs.en,
  common: {
    actions: { remove: "Remove", retry: "Retry" },
    states: { loading: "Loading" },
  },
};

function Wrapper({ children }: { children: ReactNode }) {
  return (
    <LocalizationProvider locale="en" messages={messages}>
      {children}
    </LocalizationProvider>
  );
}

beforeEach(() => {
  dictation.options = null;
  dictation.sessionKeys = [];
  dictation.start.mockClear();
  dictation.stop.mockClear();
});

afterEach(cleanup);

describe("procedure note dictation integration", () => {
  it("merges confirmed speech into the latest draft and blocks Send while recording", () => {
    render(<PatientProcedureEventItemsPanel canWrite eventId="event-1" />, {
      wrapper: Wrapper,
    });
    const composer = screen.getByRole("textbox", { name: "Clinical note" });

    fireEvent.change(composer, { target: { value: "Typed while recording" } });
    act(() => dictation.options?.onSegment(" confirmed speech "));

    expect(composer).toHaveValue("Typed while recording confirmed speech");
    expect(screen.getByRole("button", { name: "Send" })).toBeDisabled();
    expect(
      screen.getByText("Recording. Select stop when you are finished."),
    ).toHaveAttribute("role", "status");

    fireEvent.click(
      screen.getByRole("button", { name: "Stop voice dictation" }),
    );
    expect(dictation.stop).toHaveBeenCalledOnce();
  });

  it("does not render dictation or the composer for read-only users", () => {
    render(
      <PatientProcedureEventItemsPanel canWrite={false} eventId="event-1" />,
      {
        wrapper: Wrapper,
      },
    );

    expect(dictation.options).toBeNull();
    expect(
      screen.queryByRole("textbox", { name: "Clinical note" }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: "Stop voice dictation" }),
    ).not.toBeInTheDocument();
  });

  it("recreates the composer for a new event so stale drafts and sessions are released", () => {
    const { rerender } = render(
      <PatientProcedureEventItemsPanel canWrite eventId="event-1" />,
      { wrapper: Wrapper },
    );
    fireEvent.change(screen.getByRole("textbox", { name: "Clinical note" }), {
      target: { value: "Event one draft" },
    });

    rerender(<PatientProcedureEventItemsPanel canWrite eventId="event-2" />);

    expect(screen.getByRole("textbox", { name: "Clinical note" })).toHaveValue(
      "",
    );
    expect(dictation.sessionKeys).toContain("event-1");
    expect(dictation.sessionKeys).toContain("event-2");
  });
});

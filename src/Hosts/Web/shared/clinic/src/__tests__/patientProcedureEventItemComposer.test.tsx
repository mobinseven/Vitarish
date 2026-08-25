import type { ReactNode } from "react";
import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import { PatientProcedureEventItemComposer } from "../components/patients/procedures/PatientProcedureEventItemComposer";
import { clinicMessageCatalogs, LocalizationProvider } from "../localization";

const mutations = vi.hoisted(() => ({
  createFile: vi.fn(),
  createNote: vi.fn(),
}));

vi.mock("@vitarish/intelligence", () => ({
  appendConfirmedTranscript: (current: string, segment: string) =>
    `${current} ${segment}`.trim(),
  LiveDictation: () => null,
  useLiveDictation: () => ({
    available: true,
    capabilitiesLoading: false,
    failureCode: null,
    isActive: false,
    language: "en-US",
    languages: [],
    setLanguage: vi.fn(),
    start: vi.fn(),
    status: "idle",
    stop: vi.fn(),
  }),
}));

vi.mock(
  "../hooks/patients/procedures/usePatientProcedureEventItemMutations",
  () => ({
    useCreatePatientProcedureEventItemFile: () => ({
      isError: false,
      isPending: false,
      mutateAsync: mutations.createFile,
      reset: vi.fn(),
    }),
    useCreatePatientProcedureEventItemText: () => ({
      isError: false,
      isPending: false,
      mutateAsync: mutations.createNote,
    }),
  }),
);

const messages = {
  ...clinicMessageCatalogs.en,
  common: { actions: { remove: "Remove" } },
};

function Wrapper({ children }: { children: ReactNode }) {
  return (
    <LocalizationProvider locale="en" messages={messages}>
      {children}
    </LocalizationProvider>
  );
}

beforeEach(() => {
  mutations.createFile.mockReset();
  mutations.createNote.mockReset();
});

describe("patient procedure event item composer", () => {
  it("clears a successful note while retaining a failed file for retry", async () => {
    mutations.createNote.mockResolvedValue(undefined);
    mutations.createFile.mockRejectedValue(new Error("upload failed"));
    const { container } = render(
      <PatientProcedureEventItemComposer eventId="event-1" />,
      {
        wrapper: Wrapper,
      },
    );
    const file = new File(["document"], "plan.pdf", {
      type: "application/pdf",
    });
    const fileInput = container.querySelector('input[type="file"]');
    expect(fileInput).not.toBeNull();
    fireEvent.change(fileInput!, { target: { files: [file] } });
    fireEvent.change(screen.getByRole("textbox", { name: "Note" }), {
      target: { value: "Keep the treatment plan" },
    });

    fireEvent.click(screen.getByRole("button", { name: "Send" }));

    await waitFor(() =>
      expect(mutations.createFile).toHaveBeenCalledWith({
        eventId: "event-1",
        file,
      }),
    );
    expect(mutations.createNote).toHaveBeenCalledWith(
      expect.objectContaining({
        eventId: "event-1",
        content: "Keep the treatment plan",
      }),
    );
    await waitFor(() =>
      expect(
        screen.getByRole("textbox", { name: "Note" }),
      ).toHaveValue(""),
    );
    expect(screen.getByRole("button", { name: "Remove" })).toBeInTheDocument();
    expect(screen.getByRole("alert")).toHaveTextContent(
      "The file could not be uploaded. It is still ready to retry.",
    );
  });
});

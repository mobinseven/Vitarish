import type { ReactNode } from "react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { intelligenceRunAgentConversation } from "@vitarish/intelligence";
import {
  clinicDeleteOperationsAssistantConversation,
  clinicDeletePatientAssistantConversation,
  clinicResolveClinicAssistantCandidate,
} from "../api/generated/clinic/clinic";
import { ClinicAssistantWorkspace } from "../features/clinic-assistant/ClinicAssistantWorkspace";
import { operationsConversationStorageKey } from "../features/clinic-assistant/hooks";
import { LocalizationProvider, clinicMessageCatalogs } from "../localization";

let userId = "user-1";
vi.mock("@vitarish/auth", async (importOriginal) => ({
  ...(await importOriginal<typeof import("@vitarish/auth")>()),
  useAuthSession: () => ({ user: { userId } }),
}));
vi.mock("../api/generated/clinic/clinic", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../api/generated/clinic/clinic")>()),
  clinicResolveClinicAssistantCandidate: vi.fn(),
  clinicDeleteOperationsAssistantConversation: vi.fn(),
  clinicDeletePatientAssistantConversation: vi.fn(),
  useClinicGetSetupProgress: () => ({ data: undefined }),
}));
vi.mock("@vitarish/intelligence", async (importOriginal) => ({
  ...(await importOriginal<typeof import("@vitarish/intelligence")>()),
  intelligenceRunAgentConversation: vi.fn(),
}));

const text = clinicMessageCatalogs.en.clinic.assistantWorkspace;
const run = vi.mocked(intelligenceRunAgentConversation);

function reply(conversationId: string, content: string, extra: Record<string, unknown> = {}) {
  return {
    runId: "run",
    conversationId,
    result: { agentRunId: `run-${conversationId}`, messages: [{ kind: "AssistantMessage", content }], actions: [], ...extra },
  } as unknown as Awaited<ReturnType<typeof intelligenceRunAgentConversation>>;
}

function Wrapper({ children }: { children: ReactNode }) {
  const client = new QueryClient({ defaultOptions: { queries: { retry: false } } });
  return (
    <QueryClientProvider client={client}>
      <LocalizationProvider locale="en" messages={clinicMessageCatalogs.en}>{children}</LocalizationProvider>
    </QueryClientProvider>
  );
}

const renderWorkspace = () => render(<ClinicAssistantWorkspace onOpenPatient={vi.fn()} />, { wrapper: Wrapper });

describe("ClinicAssistantWorkspace", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    window.sessionStorage.clear();
  });
  afterEach(cleanup);

  it("sends a starter to the operations assistant and shows its reply", async () => {
    userId = "starter";
    run.mockResolvedValueOnce(reply("ops-1", "You have three appointments today."));
    renderWorkspace();

    fireEvent.click(screen.getByRole("button", { name: new RegExp(text.suggestions.items.todaySchedule.title) }));

    expect(await screen.findByText("You have three appointments today.")).toBeInTheDocument();
    expect(run.mock.calls[0][0]).toMatchObject({
      agentKey: "clinic.assistant.operations",
      message: text.suggestions.items.todaySchedule.prompt,
    });
    expect(screen.queryByRole("button", { name: new RegExp(text.suggestions.items.findPatient.title) })).toBeNull();
  });

  it("continues a request about a chosen patient in the same timeline and returns to the clinic", async () => {
    userId = "focus";
    run
      .mockResolvedValueOnce(reply("ops-1", "Which patient?", {
        candidates: [{ candidateToken: "token-1", displayName: "Sara Ahmadi", maskedIdentification: "***12" }],
      }))
      .mockResolvedValueOnce(reply("patient-1", "Sara's last visit was in May."));
    vi.mocked(clinicResolveClinicAssistantCandidate).mockResolvedValueOnce({ patientId: "patient-id-1" } as never);
    vi.mocked(clinicDeletePatientAssistantConversation).mockResolvedValue(undefined as never);
    renderWorkspace();

    fireEvent.click(screen.getByRole("button", { name: new RegExp(text.suggestions.items.findPatient.title) }));
    fireEvent.click(await screen.findByRole("button", { name: clinicMessageCatalogs.en.clinic.operationsAssistant.candidates.select }));

    expect(await screen.findByText("Sara's last visit was in May.")).toBeInTheDocument();
    expect(run.mock.calls[1][0]).toMatchObject({
      agentKey: "clinic.assistant.patient",
      subjectKind: "patient",
      subjectId: "patient-id-1",
      message: text.suggestions.items.findPatient.prompt,
    });
    expect(screen.getByText("Which patient?")).toBeInTheDocument();
    expect(screen.getByRole("separator", { name: "Now about Sara Ahmadi" })).toBeInTheDocument();
    expect(within(screen.getByRole("group", { name: text.focus.label })).getByText("Patient: Sara Ahmadi")).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: text.focus.clear }));

    expect(await screen.findByRole("separator", { name: text.backToClinic })).toBeInTheDocument();
    expect(screen.queryByRole("group", { name: text.focus.label })).toBeNull();
    expect(screen.getByText("Sara's last visit was in May.")).toBeInTheDocument();
    await waitFor(() => expect(clinicDeletePatientAssistantConversation).toHaveBeenCalledWith("patient-id-1", "patient-1"));
  });

  it("stops waiting without an error and keeps the conversation", async () => {
    userId = "stop";
    run.mockImplementationOnce((_request, _options, signal) => new Promise((_resolve, reject) => {
      signal?.addEventListener("abort", () => reject(new DOMException("Aborted", "AbortError")));
    }));
    renderWorkspace();

    fireEvent.click(screen.getByRole("button", { name: new RegExp(text.suggestions.items.addShift.title) }));
    fireEvent.click(await screen.findByRole("button", { name: text.stop }));

    expect(await screen.findByText(text.stopped)).toBeInTheDocument();
    expect(screen.queryByRole("alert")).toBeNull();
    expect(clinicDeleteOperationsAssistantConversation).not.toHaveBeenCalled();
  });

  it("asks before starting a new conversation, then deletes the current one", async () => {
    userId = "new";
    run.mockResolvedValueOnce(reply("ops-9", "Shift added."));
    vi.mocked(clinicDeleteOperationsAssistantConversation).mockResolvedValueOnce(undefined as never);
    renderWorkspace();

    fireEvent.click(screen.getByRole("button", { name: new RegExp(text.suggestions.items.addShift.title) }));
    await screen.findByText("Shift added.");
    fireEvent.click(screen.getByRole("button", { name: text.newConversation }));
    const dialog = await screen.findByRole("dialog");
    expect(clinicDeleteOperationsAssistantConversation).not.toHaveBeenCalled();
    fireEvent.click(within(dialog).getByRole("button", { name: text.newConversation }));

    await waitFor(() => expect(clinicDeleteOperationsAssistantConversation).toHaveBeenCalledWith("ops-9"));
    await waitFor(() => expect(screen.queryByText("Shift added.")).toBeNull());
    expect(screen.getByRole("button", { name: new RegExp(text.suggestions.items.addShift.title) })).toBeInTheDocument();
  });

  it("shows the thread again after navigating away and back", async () => {
    userId = "remount";
    run.mockResolvedValueOnce(reply("ops-5", "Two free slots on Monday."));
    const first = renderWorkspace();
    fireEvent.click(screen.getByRole("button", { name: new RegExp(text.suggestions.items.freeSlots.title) }));
    await screen.findByText("Two free slots on Monday.");
    first.unmount();

    renderWorkspace();

    expect(screen.getByText("Two free slots on Monday.")).toBeInTheDocument();
    expect(screen.queryByText(text.continuing.title)).toBeNull();
    expect(run).toHaveBeenCalledTimes(1);
  });

  it("says when it continues an earlier conversation it cannot show", () => {
    userId = "reload";
    window.sessionStorage.setItem(operationsConversationStorageKey, "ops-earlier");
    renderWorkspace();

    expect(screen.getByText(text.continuing.title)).toBeInTheDocument();
    expect(screen.getByRole("button", { name: text.newConversation })).toBeEnabled();
  });
});

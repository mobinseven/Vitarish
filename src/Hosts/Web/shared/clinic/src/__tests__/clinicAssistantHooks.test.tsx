import type { ReactNode } from "react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, renderHook } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
  clinicConfirmPatientAssistantAction,
  clinicConfirmAssistantAction,
  clinicDeleteOperationsAssistantConversation,
  clinicGetAssistantAction,
  clinicGetPatientAssistantAction,
  clinicRejectAssistantAction,
  clinicRejectPatientAssistantAction,
} from "../api/generated/clinic/clinic";
import { intelligenceRunAgentConversation } from "@vitarish/intelligence";
import {
  useConfirmAssistantAction,
  useConfirmOperationsAssistantAction,
  useOperationsAssistantAction,
  useOperationsAssistantConversation,
  useAssistantAction,
  useAssistantConversation,
  useRejectOperationsAssistantAction,
  useRejectAssistantAction,
} from "../features/clinic-assistant/hooks";

vi.mock("../api/generated/clinic/clinic", () => ({
  clinicDeletePatientAssistantConversation: vi.fn(),
  clinicDeleteOperationsAssistantConversation: vi.fn(),
  clinicGetAssistantAction: vi.fn(),
  clinicConfirmAssistantAction: vi.fn(),
  clinicRejectAssistantAction: vi.fn(),
  clinicGetPatientAssistantAction: vi.fn(),
  clinicConfirmPatientAssistantAction: vi.fn(),
  clinicRejectPatientAssistantAction: vi.fn(),
  clinicReviseAssistantAction: vi.fn(),
  getClinicGetSetupProgressQueryKey: () => ["clinic", "setup-progress"],
}));

vi.mock("@vitarish/intelligence", async () => ({
  ...(await vi.importActual<typeof import("@vitarish/intelligence")>("@vitarish/intelligence")),
  intelligenceRunAgentConversation: vi.fn(),
}));

function createWrapper() {
  const client = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return function Wrapper({ children }: { children: ReactNode }) {
    return (
      <QueryClientProvider client={client}>{children}</QueryClientProvider>
    );
  };
}

describe("clinic-assistant generated-client hooks", () => {
  beforeEach(() => vi.clearAllMocks());

  it("fixes the patient scope and continues the returned conversation", async () => {
    vi.mocked(intelligenceRunAgentConversation)
      .mockResolvedValueOnce({
        agentRunId: "run-1",
        conversationId: "conversation-1",
        result: { messages: [], actions: [] },
      })
      .mockResolvedValueOnce({
        agentRunId: "run-2",
        conversationId: "conversation-1",
        result: { messages: [], actions: [] },
      });
    const { result } = renderHook(
      () => useAssistantConversation("patient-from-route", true),
      { wrapper: createWrapper() },
    );

    await act(() => result.current.submit("First request"));
    await act(() => result.current.submit("Continue"));

    expect(intelligenceRunAgentConversation).toHaveBeenNthCalledWith(
      1,
      {
        agentKey: "clinic.assistant.patient",
        subjectKind: "patient",
        subjectId: "patient-from-route",
        message: "First request",
        conversationId: null,
      },
      undefined,
      expect.any(AbortSignal),
    );
    expect(intelligenceRunAgentConversation).toHaveBeenNthCalledWith(
      2,
      {
        agentKey: "clinic.assistant.patient",
        subjectKind: "patient",
        subjectId: "patient-from-route",
        message: "Continue",
        conversationId: "conversation-1",
      },
      undefined,
      expect.any(AbortSignal),
    );
  });

  it("retrieves backend-owned action review data", async () => {
    vi.mocked(clinicGetPatientAssistantAction).mockResolvedValue({
      actionId: "action-1",
      agentRunId: "run-1",
      conversationId: "conversation-1",
      capability: "Appointments",
      actionType: "create_appointment",
      status: "Proposed",
      reviewPayload: {},
      expiresAt: "2026-08-01T12:00:00Z",
      completionPayload: null,
      failure: null,
    });
    const { result } = renderHook(
      () => useAssistantAction("patient-1", "action-1"),
      { wrapper: createWrapper() },
    );
    await vi.waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(clinicGetPatientAssistantAction).toHaveBeenCalledWith(
      "patient-1",
      "action-1",
      undefined,
      expect.any(AbortSignal),
    );
  });

  it("keeps the operations conversation in its practice scope", async () => {
    vi.mocked(intelligenceRunAgentConversation).mockResolvedValue({
      agentRunId: "run-operations",
      conversationId: "conversation-operations",
      result: { messages: [], actions: [] },
    });
    vi.mocked(clinicDeleteOperationsAssistantConversation).mockResolvedValue();
    const { result } = renderHook(
      () => useOperationsAssistantConversation(true),
      { wrapper: createWrapper() },
    );

    await act(() => result.current.submit("Show today's schedule"));
    await act(() => result.current.deleteCurrent());

    expect(intelligenceRunAgentConversation).toHaveBeenCalledWith(
      {
        agentKey: "clinic.assistant.operations",
        subjectKind: "practice",
        subjectId: "practice",
        message: "Show today's schedule",
        conversationId: null,
      },
      undefined,
      expect.any(AbortSignal),
    );
    expect(clinicDeleteOperationsAssistantConversation).toHaveBeenCalledTimes(1);
    expect(clinicDeleteOperationsAssistantConversation).toHaveBeenCalledWith(
      "conversation-operations",
    );
  });

  it("uses operations action endpoints independently of patient actions", async () => {
    vi.mocked(clinicGetAssistantAction).mockResolvedValue({
      actionId: "operation-1",
      agentRunId: "run-1",
      conversationId: "conversation-1",
      capability: "Operations",
      actionType: "create_shift",
      status: "Proposed",
      reviewPayload: {},
      expiresAt: "2026-08-01T12:00:00Z",
      completionPayload: null,
      failure: null,
    });
    vi.mocked(clinicConfirmAssistantAction).mockResolvedValue({
      actionId: "operation-1",
      status: "Completed",
      completionPayload: {},
      failure: null,
    });
    vi.mocked(clinicRejectAssistantAction).mockResolvedValue({
      actionId: "operation-2",
      status: "Rejected",
    });
    const wrapper = createWrapper();
    const action = renderHook(() => useOperationsAssistantAction("operation-1"), {
      wrapper,
    });
    await vi.waitFor(() => expect(action.result.current.isSuccess).toBe(true));
    const confirm = renderHook(() => useConfirmOperationsAssistantAction(), {
      wrapper,
    });
    const reject = renderHook(() => useRejectOperationsAssistantAction(), {
      wrapper,
    });

    await act(() => confirm.result.current.mutateAsync("operation-1"));
    await act(() => reject.result.current.mutateAsync("operation-2"));

    expect(clinicGetAssistantAction).toHaveBeenCalledWith(
      "operation-1",
      undefined,
      expect.any(AbortSignal),
    );
    expect(clinicConfirmAssistantAction).toHaveBeenCalledWith("operation-1");
    expect(clinicRejectAssistantAction).toHaveBeenCalledWith("operation-2", {
      actionId: "operation-2",
    });
  });

  it("sends only the backend action identifier to confirm and reject", async () => {
    vi.mocked(clinicConfirmPatientAssistantAction).mockResolvedValue({
      actionId: "action-1",
      status: "Completed",
      completionPayload: {},
      failure: null,
    });
    vi.mocked(clinicRejectPatientAssistantAction).mockResolvedValue({
      actionId: "action-2",
      status: "Rejected",
    });
    const wrapper = createWrapper();
    const confirm = renderHook(
      () => useConfirmAssistantAction("patient-1"),
      { wrapper },
    );
    const reject = renderHook(() => useRejectAssistantAction("patient-1"), {
      wrapper,
    });

    await act(() => confirm.result.current.mutateAsync("action-1"));
    await act(() => reject.result.current.mutateAsync("action-2"));

    expect(clinicConfirmPatientAssistantAction).toHaveBeenCalledWith(
      "patient-1",
      "action-1",
      { actionId: "action-1" },
    );
    expect(clinicRejectPatientAssistantAction).toHaveBeenCalledWith(
      "patient-1",
      "action-2",
      { actionId: "action-2" },
    );
  });
});

describe("operations assistant conversation across surfaces", () => {
  const storageKey = "vitarish.clinic.operationsAssistant.conversationId";
  const reply = (conversationId: string) => ({
    agentRunId: `run-${conversationId}`,
    conversationId,
    result: { messages: [], actions: [] },
  });

  beforeEach(() => {
    vi.clearAllMocks();
    window.sessionStorage.clear();
  });
  afterEach(() => {
    vi.restoreAllMocks();
    window.sessionStorage.clear();
  });

  it("lets a second mounted surface continue the conversation the first one started", async () => {
    vi.mocked(intelligenceRunAgentConversation)
      .mockResolvedValueOnce(reply("shared-conversation"))
      .mockResolvedValueOnce(reply("shared-conversation"));
    const wrapper = createWrapper();
    const sidePanel = renderHook(() => useOperationsAssistantConversation(true), { wrapper });
    await act(() => sidePanel.result.current.submit("first question"));
    expect(window.sessionStorage.getItem(storageKey)).toBe("shared-conversation");

    const setupHome = renderHook(() => useOperationsAssistantConversation(true), { wrapper });
    await act(() => setupHome.result.current.submit("follow-up"));

    expect(vi.mocked(intelligenceRunAgentConversation).mock.calls[0][0]).toMatchObject({ conversationId: null });
    expect(vi.mocked(intelligenceRunAgentConversation).mock.calls[1][0]).toMatchObject({
      message: "follow-up",
      conversationId: "shared-conversation",
    });
    expect(clinicDeleteOperationsAssistantConversation).not.toHaveBeenCalled();
  });

  it("forgets the stored conversation once it is deleted", async () => {
    vi.mocked(intelligenceRunAgentConversation).mockResolvedValue(reply("to-delete"));
    vi.mocked(clinicDeleteOperationsAssistantConversation).mockResolvedValue();
    const wrapper = createWrapper();
    const first = renderHook(() => useOperationsAssistantConversation(true), { wrapper });
    await act(() => first.result.current.submit("hello"));
    await act(() => first.result.current.deleteCurrent());
    expect(window.sessionStorage.getItem(storageKey)).toBeNull();

    const second = renderHook(() => useOperationsAssistantConversation(true), { wrapper });
    await act(() => second.result.current.submit("fresh start"));

    expect(vi.mocked(intelligenceRunAgentConversation).mock.calls.at(-1)?.[0]).toMatchObject({ conversationId: null });
  });

  it("keeps the conversation open across a panel close", async () => {
    vi.mocked(intelligenceRunAgentConversation).mockResolvedValue(reply("kept"));
    const { result, rerender } = renderHook(
      ({ isOpen }) => useOperationsAssistantConversation(isOpen),
      { wrapper: createWrapper(), initialProps: { isOpen: true } },
    );
    await act(() => result.current.submit("first"));

    rerender({ isOpen: false });
    rerender({ isOpen: true });
    await act(() => result.current.submit("second"));

    expect(clinicDeleteOperationsAssistantConversation).not.toHaveBeenCalled();
    expect(vi.mocked(intelligenceRunAgentConversation).mock.calls[1][0]).toMatchObject({ conversationId: "kept" });
  });

  it("still converses within one surface when session storage throws", async () => {
    vi.spyOn(Storage.prototype, "getItem").mockImplementation(() => {
      throw new Error("storage blocked");
    });
    vi.spyOn(Storage.prototype, "setItem").mockImplementation(() => {
      throw new Error("storage blocked");
    });
    vi.mocked(intelligenceRunAgentConversation)
      .mockResolvedValueOnce(reply("in-memory"))
      .mockResolvedValueOnce(reply("in-memory"));
    const { result } = renderHook(() => useOperationsAssistantConversation(true), { wrapper: createWrapper() });

    await act(() => result.current.submit("first"));
    await act(() => result.current.submit("second"));

    expect(result.current.isError).toBe(false);
    expect(vi.mocked(intelligenceRunAgentConversation).mock.calls[0][0]).toMatchObject({ conversationId: null });
    expect(vi.mocked(intelligenceRunAgentConversation).mock.calls[1][0]).toMatchObject({ conversationId: "in-memory" });
  });
});

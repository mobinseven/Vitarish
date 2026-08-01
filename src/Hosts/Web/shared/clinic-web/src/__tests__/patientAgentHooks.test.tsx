import type { ReactNode } from "react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { act, renderHook } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  clinicConfirmPatientAgentAction,
  clinicConfirmAgentAction,
  clinicDeleteOperationsAgentConversation,
  clinicGetAgentAction,
  clinicGetPatientAgentAction,
  clinicRejectAgentAction,
  clinicRejectPatientAgentAction,
} from "../api/generated/clinic/clinic";
import { intelligenceRunAgentConversation } from "@vitarish/intelligence-web";
import {
  useConfirmPatientAgentAction,
  useConfirmOperationsAgentAction,
  useOperationsAgentAction,
  useOperationsAgentConversation,
  usePatientAgentAction,
  usePatientAgentConversation,
  useRejectOperationsAgentAction,
  useRejectPatientAgentAction,
} from "../features/patient-agent/hooks";

vi.mock("../api/generated/clinic/clinic", () => ({
  clinicDeletePatientAgentConversation: vi.fn(),
  clinicDeleteOperationsAgentConversation: vi.fn(),
  clinicGetAgentAction: vi.fn(),
  clinicConfirmAgentAction: vi.fn(),
  clinicRejectAgentAction: vi.fn(),
  clinicGetPatientAgentAction: vi.fn(),
  clinicConfirmPatientAgentAction: vi.fn(),
  clinicRejectPatientAgentAction: vi.fn(),
}));

vi.mock("@vitarish/intelligence-web", async () => ({
  ...(await vi.importActual<typeof import("@vitarish/intelligence-web")>("@vitarish/intelligence-web")),
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

describe("patient-agent generated-client hooks", () => {
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
      () => usePatientAgentConversation("patient-from-route", true),
      { wrapper: createWrapper() },
    );

    await act(() => result.current.submit("First request"));
    await act(() => result.current.submit("Continue"));

    expect(intelligenceRunAgentConversation).toHaveBeenNthCalledWith(
      1,
      {
        agentKey: "clinic.patient",
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
        agentKey: "clinic.patient",
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
    vi.mocked(clinicGetPatientAgentAction).mockResolvedValue({
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
      () => usePatientAgentAction("patient-1", "action-1"),
      { wrapper: createWrapper() },
    );
    await vi.waitFor(() => expect(result.current.isSuccess).toBe(true));
    expect(clinicGetPatientAgentAction).toHaveBeenCalledWith(
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
    vi.mocked(clinicDeleteOperationsAgentConversation).mockResolvedValue();
    const { result } = renderHook(
      () => useOperationsAgentConversation(true),
      { wrapper: createWrapper() },
    );

    await act(() => result.current.submit("Show today's schedule"));
    await act(() => result.current.deleteCurrent());

    expect(intelligenceRunAgentConversation).toHaveBeenCalledWith(
      {
        agentKey: "clinic.operations",
        subjectKind: "practice",
        subjectId: "practice",
        message: "Show today's schedule",
        conversationId: null,
      },
      undefined,
      expect.any(AbortSignal),
    );
    expect(clinicDeleteOperationsAgentConversation).toHaveBeenCalledTimes(1);
    expect(clinicDeleteOperationsAgentConversation).toHaveBeenCalledWith(
      "conversation-operations",
    );
  });

  it("uses operations action endpoints independently of patient actions", async () => {
    vi.mocked(clinicGetAgentAction).mockResolvedValue({
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
    vi.mocked(clinicConfirmAgentAction).mockResolvedValue({
      actionId: "operation-1",
      status: "Completed",
      completionPayload: {},
      failure: null,
    });
    vi.mocked(clinicRejectAgentAction).mockResolvedValue({
      actionId: "operation-2",
      status: "Rejected",
    });
    const wrapper = createWrapper();
    const action = renderHook(() => useOperationsAgentAction("operation-1"), {
      wrapper,
    });
    await vi.waitFor(() => expect(action.result.current.isSuccess).toBe(true));
    const confirm = renderHook(() => useConfirmOperationsAgentAction(), {
      wrapper,
    });
    const reject = renderHook(() => useRejectOperationsAgentAction(), {
      wrapper,
    });

    await act(() => confirm.result.current.mutateAsync("operation-1"));
    await act(() => reject.result.current.mutateAsync("operation-2"));

    expect(clinicGetAgentAction).toHaveBeenCalledWith(
      "operation-1",
      undefined,
      expect.any(AbortSignal),
    );
    expect(clinicConfirmAgentAction).toHaveBeenCalledWith("operation-1");
    expect(clinicRejectAgentAction).toHaveBeenCalledWith("operation-2", {
      actionId: "operation-2",
    });
  });

  it("sends only the backend action identifier to confirm and reject", async () => {
    vi.mocked(clinicConfirmPatientAgentAction).mockResolvedValue({
      actionId: "action-1",
      status: "Completed",
      completionPayload: {},
      failure: null,
    });
    vi.mocked(clinicRejectPatientAgentAction).mockResolvedValue({
      actionId: "action-2",
      status: "Rejected",
    });
    const wrapper = createWrapper();
    const confirm = renderHook(
      () => useConfirmPatientAgentAction("patient-1"),
      { wrapper },
    );
    const reject = renderHook(() => useRejectPatientAgentAction("patient-1"), {
      wrapper,
    });

    await act(() => confirm.result.current.mutateAsync("action-1"));
    await act(() => reject.result.current.mutateAsync("action-2"));

    expect(clinicConfirmPatientAgentAction).toHaveBeenCalledWith(
      "patient-1",
      "action-1",
      { actionId: "action-1" },
    );
    expect(clinicRejectPatientAgentAction).toHaveBeenCalledWith(
      "patient-1",
      "action-2",
      { actionId: "action-2" },
    );
  });
});

import React from "react";
import { cleanup, fireEvent, render, screen, waitFor, within } from "@testing-library/react";
import { I18nProvider } from "@vitarish/localization";
import { ApiError, type LandingWorkspacesResponse } from "@vitarish/platform";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { homeMessageCatalogs } from "../homeMessages";
import { HomeWorkspaces } from "../HomeWorkspaces";
import { PlatformHome } from "../PlatformHome";

const mocks = vi.hoisted(() => ({
  session: {
    isAuthenticated: true,
    isLoading: false,
    isError: false,
    displayProfile: { givenName: "Ali" },
    refetch: vi.fn(),
  },
  push: vi.fn(),
  logout: vi.fn(),
  logoutErrorMessage: null as string | null,
  runHome: vi.fn(),
  deleteHome: vi.fn(),
  resetConversation: vi.fn(),
  directory: undefined as LandingWorkspacesResponse | undefined,
  handoffStart: vi.fn(),
}));

vi.mock("next/navigation", () => ({ useRouter: () => ({ push: mocks.push }) }));

vi.mock("@heroui/react", () => {
  const Button = ({ children, isDisabled, isPending, onPress, ...props }: any) => (
    <button type="button" {...props} disabled={isDisabled || isPending} onClick={onPress}>{children}</button>
  );
  const Tooltip: any = ({ children }: any) => <>{children}</>;
  Tooltip.Content = ({ children }: any) => <span data-testid="tooltip">{children}</span>;
  const Alert: any = ({ children }: any) => <div role="alert">{children}</div>;
  Alert.Indicator = ({ children }: any) => <span>{children}</span>;
  Alert.Content = ({ children }: any) => <div>{children}</div>;
  Alert.Title = ({ children }: any) => <p>{children}</p>;
  Alert.Description = ({ children }: any) => <div>{children}</div>;
  const Card: any = ({ children }: any) => <div>{children}</div>;
  Card.Header = ({ children }: any) => <div>{children}</div>;
  Card.Title = ({ children }: any) => <h2>{children}</h2>;
  Card.Content = ({ children }: any) => <div>{children}</div>;
  Card.Footer = ({ children }: any) => <div>{children}</div>;

  return {
    Alert,
    Button,
    Card,
    Chip: ({ children }: any) => <span>{children}</span>,
    Spinner: ({ children, ...props }: any) => <span {...props}>{children}</span>,
    Tooltip,
  };
});

vi.mock("@vitarish/auth", () => ({
  useAuthSession: () => mocks.session,
  useAuthErrorDisplay: (_error: unknown, fallbackMessage: string | null) => fallbackMessage,
  useLogout: () => ({ logout: mocks.logout, error: null, errorMessage: mocks.logoutErrorMessage, isLoading: false, isContextLoading: false }),
}));

vi.mock("@vitarish/intelligence", async () => {
  const React = await import("react");
  function useConversationController(options: any) {
    const [state, setState] = React.useState<any>({
      isPending: false,
      isError: false,
      conversationId: null,
      lastMessage: null,
    });
    const submit = React.useCallback(async (message: string) => {
      setState((current: any) => ({ ...current, isPending: true, isError: false, lastMessage: message }));
      try {
        const result = await options.run(message, state.conversationId, new AbortController().signal);
        setState((current: any) => ({ ...current, isPending: false, isError: false, conversationId: result.conversationId }));
        return result.result;
      } catch {
        setState((current: any) => ({ ...current, isPending: false, isError: true }));
        return undefined;
      }
    }, [options, state.conversationId]);
    const retry = React.useCallback(
      () => state.lastMessage ? submit(state.lastMessage) : Promise.resolve(undefined),
      [state.lastMessage, submit],
    );
    const reset = React.useCallback(() => {
      mocks.resetConversation();
      setState({ isPending: false, isError: false, conversationId: null, lastMessage: null });
    }, []);
    return { ...state, submit, retry, reset };
  }
  return {
    AgentConversationComposer: ({ label, placeholder, submitLabel, value, onChange, onSubmit }: any) => (
      <div>
        <textarea aria-label={label} placeholder={placeholder} value={value} onChange={(event) => onChange(event.target.value)} />
        <button type="button" onClick={() => void onSubmit(value)}>{submitLabel}</button>
      </div>
    ),
    InteractiveReplyMessage: () => null,
    toInteractiveReply: (reply: unknown) => reply,
    useConversationController,
  };
});

vi.mock("@vitarish/platform", () => {
  class MockApiError extends Error {
    status: number;
    code: string | undefined;
    problem: Record<string, unknown>;

    constructor(problem: { status: number; code?: string }) {
      super("Request failed");
      this.status = problem.status;
      this.code = problem.code;
      this.problem = problem;
    }
  }

  return {
    ApiError: MockApiError,
    LandingAppAvailability: { Live: "Live", Soon: "Soon" },
    landingDeleteHomeConversation: mocks.deleteHome,
    landingRunHomeConversation: mocks.runHome,
    useLandingListWorkspaces: () => ({ data: mocks.directory, isLoading: false, isError: false, refetch: vi.fn() }),
  };
});

vi.mock("@vitarish/ui", () => ({
  ChatConversation: ({ children }: any) => <div>{children}</div>,
  ChatLoadingMessage: () => null,
  ChatMessage: ({ children }: any) => <div>{children}</div>,
  FaIcon: () => null,
}));

vi.mock("../useHomeHandoff", () => ({
  useHomeHandoff: () => ({ pending: null, failed: null, start: mocks.handoffStart }),
}));

const directory = {
  workspaces: [{ tenantId: "tenant-1", appId: "darman", name: "Family Clinic", hostname: "family.localhost", hasLogo: false }],
  apps: [{ appId: "darman", name: "Darman", availability: "Live", canCreate: true }],
  actionKeys: { openWorkspace: "open-workspace", createWorkspace: "create-workspace" },
} as unknown as LandingWorkspacesResponse;

function renderPlatformHome() {
  return render(
    <I18nProvider locale="en" messages={homeMessageCatalogs.en}>
      <PlatformHome signInHref="https://auth.example.test/login" />
    </I18nProvider>,
  );
}

describe("platform home interactions", () => {
  afterEach(cleanup);

  beforeEach(() => {
    mocks.session = {
      isAuthenticated: true,
      isLoading: false,
      isError: false,
      displayProfile: { givenName: "Ali" },
      refetch: vi.fn().mockResolvedValue(undefined),
    };
    mocks.directory = directory;
    mocks.push.mockReset();
    mocks.logout.mockReset();
    mocks.logoutErrorMessage = null;
    mocks.runHome.mockReset().mockResolvedValue({
      conversationId: "conversation-1",
      message: "Here is a helpful answer.",
      reply: { text: "Here is a helpful answer." },
      actionTarget: null,
    });
    mocks.deleteHome.mockReset().mockResolvedValue(undefined);
    mocks.resetConversation.mockReset();
    mocks.handoffStart.mockReset();
  });

  it("shows session lookup failure and retries the session request without redirecting", async () => {
    const refetch = vi.fn().mockResolvedValue(undefined);
    mocks.session = { ...mocks.session, isAuthenticated: false, isError: true, refetch };
    renderPlatformHome();

    expect(screen.getByRole("alert").textContent).toContain("Your session could not be checked.");
    await fireEvent.click(screen.getByRole("button", { name: "Try again" }));

    expect(refetch).toHaveBeenCalledTimes(1);
    expect(screen.queryByRole("textbox", { name: "Message Vitarish" })).toBeNull();
  });

  it("keeps a failed draft and retry adds its person turn only once", async () => {
    mocks.runHome.mockRejectedValueOnce(new Error("temporary failure"));
    renderPlatformHome();

    const composer = screen.getByRole("textbox", { name: "Message Vitarish" });
    fireEvent.change(composer, { target: { value: "How do I open my workspace?" } });
    fireEvent.click(screen.getByRole("button", { name: "Send" }));

    await waitFor(() => expect(screen.queryByRole("button", { name: "Try again" })).not.toBeNull());
    expect((composer as HTMLTextAreaElement).value).toBe("How do I open my workspace?");

    fireEvent.click(screen.getByRole("button", { name: "Try again" }));
    await waitFor(() => expect(screen.queryByText("How do I open my workspace?")).not.toBeNull());

    expect(mocks.runHome).toHaveBeenCalledTimes(2);
    expect(screen.getAllByText("How do I open my workspace?")).toHaveLength(1);
    expect((composer as HTMLTextAreaElement).value).toBe("");
  });

  it("offers a fresh conversation after a conflict and clears the old turns", async () => {
    renderPlatformHome();
    const composer = screen.getByRole("textbox", { name: "Message Vitarish" });
    fireEvent.change(composer, { target: { value: "First question" } });
    fireEvent.click(screen.getByRole("button", { name: "Send" }));
    await waitFor(() => expect(screen.queryByText("First question")).not.toBeNull());

    mocks.runHome.mockRejectedValueOnce(new ApiError({ status: 409 }));
    fireEvent.change(composer, { target: { value: "Question after conflict" } });
    fireEvent.click(screen.getByRole("button", { name: "Send" }));
    await waitFor(() => expect(screen.queryByRole("button", { name: "Start a new conversation" })).not.toBeNull());

    fireEvent.click(screen.getByRole("button", { name: "Start a new conversation" }));

    expect(mocks.resetConversation).toHaveBeenCalledTimes(1);
    expect(screen.queryByText("First question")).toBeNull();
    expect((composer as HTMLTextAreaElement).value).toBe("Question after conflict");
  });

  it("signs out to the landing front door", async () => {
    mocks.logoutErrorMessage = "Sign-out failed.";
    renderPlatformHome();

    expect(screen.getByRole("alert").textContent).toContain("Sign-out failed.");
    fireEvent.click(screen.getByRole("button", { name: "Sign out" }));

    expect(mocks.logout).toHaveBeenCalledWith("/");
  });

  it("groups workspaces by app and hands off the workspace named by its accessible label", async () => {
    const groupedDirectory = {
      ...directory,
      workspaces: [
        ...directory.workspaces,
        { tenantId: "tenant-2", appId: "second-app", name: "Second Workspace", hostname: "second.localhost", hasLogo: false },
      ],
      apps: [
        ...directory.apps,
        { appId: "second-app", name: "Second App", availability: "Live", canCreate: false },
      ],
    } as unknown as LandingWorkspacesResponse;

    render(
      <I18nProvider locale="en" messages={homeMessageCatalogs.en}>
        <HomeWorkspaces
          directory={groupedDirectory}
          isLoading={false}
          loadFailed={false}
          onRetry={vi.fn()}
          pending={null}
          failed={null}
          onStart={mocks.handoffStart}
        />
      </I18nProvider>,
    );

    expect(screen.getByRole("heading", { name: "Darman" })).not.toBeNull();
    expect(screen.getByRole("heading", { name: "Second App" })).not.toBeNull();
    const darmanGroup = screen.getByRole("heading", { name: "Darman" }).closest("li");
    const secondAppGroup = screen.getByRole("heading", { name: "Second App" }).closest("li");
    expect(darmanGroup).not.toBeNull();
    expect(secondAppGroup).not.toBeNull();
    expect(within(darmanGroup as HTMLElement).getByRole("button", { name: "Open Family Clinic" })).not.toBeNull();
    expect(within(secondAppGroup as HTMLElement).getByRole("button", { name: "Open Second Workspace" })).not.toBeNull();
    const isolatedNames = screen.getAllByText("Family Clinic");
    expect(isolatedNames.length).toBeGreaterThan(0);
    expect(isolatedNames.every((name) => name.tagName.toLowerCase() === "bdi")).toBe(true);
    fireEvent.click(screen.getByRole("button", { name: "Open Family Clinic" }));

    expect(mocks.handoffStart).toHaveBeenCalledWith({ kind: "open", tenantId: "tenant-1" });
  });
});

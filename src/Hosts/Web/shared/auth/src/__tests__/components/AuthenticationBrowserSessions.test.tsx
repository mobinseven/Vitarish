import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, expect, it, vi } from "vitest";
import { LocalizationProvider } from "@vitarish/localization";
import { AuthenticationBrowserSessions } from "../../components/AuthenticationBrowserSessions";
import { authMessageCatalogs } from "../../localization/catalogs";

const actions = vi.hoisted(() => ({ revoke: vi.fn(), refetch: vi.fn() }));
vi.mock("../../api/generated/authentication/authentication", () => ({
  authenticationBrowserSessionRevoke: actions.revoke,
  useAuthenticationBrowserSessionsGet: () => ({
    data: { sessions: [{ sessionId: "another-browser", isCurrent: false, createdAt: "2026-11-07T10:00:00Z", lastActivityAt: "2026-11-07T10:01:00Z" }] },
    isLoading: false, isError: false, refetch: actions.refetch,
  }),
}));

afterEach(() => { cleanup(); vi.clearAllMocks(); });

it("keeps a failed revocation visible, prevents duplicate submissions, and refreshes after success", async () => {
  let fail!: (error: Error) => void;
  actions.revoke.mockImplementationOnce(() => new Promise((_, reject) => { fail = reject; }));
  render(<LocalizationProvider locale="en" messages={authMessageCatalogs.en}><AuthenticationBrowserSessions /></LocalizationProvider>);
  const button = screen.getByRole("button", { name: authMessageCatalogs.en.auth.browserSessions.end });
  fireEvent.click(button);
  await waitFor(() => expect(actions.revoke).toHaveBeenCalledWith({ sessionId: "another-browser" }));
  expect(button).toBeDisabled();
  fireEvent.click(button);
  expect(actions.revoke).toHaveBeenCalledTimes(1);
  fail(new Error("Unavailable"));
  await screen.findByText(authMessageCatalogs.en.auth.failures.providerFailure);
  expect(screen.getByText(authMessageCatalogs.en.auth.browserSessions.other)).toBeVisible();
  expect(actions.refetch).not.toHaveBeenCalled();
  await waitFor(() => expect(button).toBeEnabled());
  actions.revoke.mockResolvedValueOnce(undefined);
  fireEvent.click(button);
  await waitFor(() => expect(actions.refetch).toHaveBeenCalledTimes(1));
  expect(actions.revoke).toHaveBeenCalledTimes(2);
  expect(screen.queryByText(authMessageCatalogs.en.auth.failures.providerFailure)).toBeNull();
});

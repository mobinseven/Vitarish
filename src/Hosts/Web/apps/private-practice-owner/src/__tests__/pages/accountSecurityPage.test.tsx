import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import AccountSecurityPage from "@/app/account/security/page";

vi.mock("@vitarish/auth", () => ({
  AuthenticationSessionNoticeType: { parallel_session_established: "parallel_session_established" },
  useAuthSession: () => ({ isAuthenticated: true }),
  useAuthenticationAccessHistory: () => ({
    data: {
      previousSuccessfulAt: "2026-08-29T08:00:00Z",
      lastSuccessfulAt: "2026-08-30T08:00:00Z",
      lastUnsuccessfulAt: "2026-08-29T07:59:00Z",
      previousSuccessfulIpAddress: null,
      lastSuccessfulIpAddress: "198.51.100.7",
      lastUnsuccessfulIpAddress: "203.0.113.9",
      failedAttemptsSincePreviousSuccessful: 2,
      currentFailedAttempts: 1,
      acknowledgedAt: null,
    },
    isLoading: false,
    isError: false,
  }),
  useAuthenticationSessionNotices: () => ({
    data: [{ id: "notice-1", createdAt: "2026-08-30T08:00:00Z", noticeType: "parallel_session_established" }],
    isLoading: false,
    isError: false,
  }),
}));

vi.mock("@/components/account/AccountSurface", async () => {
  const { StandaloneDocumentPage } = await import("@vitarish/ui");
  return {
    AccountSurface: ({ children }: { children: React.ReactNode }) => (
      <StandaloneDocumentPage>{children}</StandaloneDocumentPage>
    ),
    AccountBackLink: () => <a href="/account">Back</a>,
  };
});

vi.mock("@/localization", () => ({
  formatDateTime: (value: string) => value,
  useLocalizedText: () => ({
    locale: "en",
    t: (key: string, values?: Record<string, string>) => (({
      "account.security.title": "Security history",
      "account.security.description": "Review security activity.",
      "account.security.accessHistory": "Access history",
      "account.security.previousSuccessful": "Previous successful sign-in",
      "account.security.lastSuccessful": "Most recent successful sign-in",
      "account.security.lastUnsuccessful": "Most recent unsuccessful sign-in",
      "account.security.failedAttempts": "Unsuccessful attempts since the previous success",
      "account.security.currentFailedAttempts": "Current unsuccessful attempts",
      "account.security.notRecorded": "Not recorded",
      "account.security.ipAddress": "IP address: {address}",
      "account.security.notices": "Session notices",
      "account.security.parallelNotice": "A new simultaneous session was established.",
      "common.states.loading": "Loading...",
    } as Record<string, string>)[key] ?? key).replace(/\{(\w+)\}/g, (_, name: string) => values?.[name] ?? ""),
  }),
}));

describe("AccountSecurityPage", () => {
  it("keeps access history and session notices visible in the authenticated area", () => {
    render(<AccountSecurityPage />);

    expect(screen.getByRole("heading", { name: "Security history" })).toBeInTheDocument();
    expect(screen.getByText("2026-08-29T08:00:00Z")).toBeInTheDocument();
    expect(screen.getByText("Unsuccessful attempts since the previous success")).toBeInTheDocument();
    expect(screen.getByText("Current unsuccessful attempts")).toBeInTheDocument();
    expect(screen.getByText("A new simultaneous session was established.")).toBeInTheDocument();
  });

  it("shows the IP address recorded for each sign-in attempt and none where it was not recorded", () => {
    render(<AccountSecurityPage />);

    const tile = (label: string) => screen.getByText(label).parentElement!;
    expect(tile("Most recent successful sign-in")).toHaveTextContent("IP address: \u2066198.51.100.7\u2069");
    expect(tile("Most recent unsuccessful sign-in")).toHaveTextContent("IP address: \u2066203.0.113.9\u2069");
    expect(tile("Previous successful sign-in")).not.toHaveTextContent("IP address");
  });
});

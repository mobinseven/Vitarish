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

vi.mock("@/components/account/AccountSurface", () => ({
  AccountSurface: ({ children }: { children: React.ReactNode }) => <main>{children}</main>,
  AccountBackLink: () => <a href="/account">Back</a>,
}));

vi.mock("@/localization", () => ({
  formatDateTime: (value: string) => value,
  useLocalizedText: () => ({
    locale: "en",
    t: (key: string) => ({
      "account.security.title": "Security history",
      "account.security.description": "Review security activity.",
      "account.security.accessHistory": "Access history",
      "account.security.previousSuccessful": "Previous successful sign-in",
      "account.security.lastSuccessful": "Most recent successful sign-in",
      "account.security.lastUnsuccessful": "Most recent unsuccessful sign-in",
      "account.security.failedAttempts": "Unsuccessful attempts since the previous success",
      "account.security.currentFailedAttempts": "Current unsuccessful attempts",
      "account.security.notRecorded": "Not recorded",
      "account.security.notices": "Session notices",
      "account.security.parallelNotice": "A new simultaneous session was established.",
      "common.states.loading": "Loading...",
    }[key] ?? key),
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
});

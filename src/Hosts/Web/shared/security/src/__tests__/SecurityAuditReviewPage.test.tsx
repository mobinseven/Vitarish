import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { LocalizationProvider } from "@vitarish/localization";
import { securityMessageCatalogs } from "../localization";
import { SecurityAuditReviewPage } from "../components/SecurityAuditReviewPage";
import { useAuditSecurityEventsList } from "../api/generated/audit/audit";

vi.mock("../api/generated/audit/audit", () => ({
  useAuditSecurityEventsList: vi.fn(),
}));

afterEach(cleanup);

describe("SecurityAuditReviewPage", () => {
  it("renders the server-provided warning and audit fields without exposing a mutation control", () => {
    vi.mocked(useAuditSecurityEventsList).mockReturnValue({
      data: {
        items: [{
          id: "event-1",
          eventName: "authentication.password.login",
          eventCategory: "authentication",
          outcome: "succeeded",
          actorKind: "user",
          actorId: "user-1",
          sourceIpKind: "client",
          sourceIpAddress: "203.0.113.7",
          occurredAt: "2026-09-02T07:00:00.000Z",
        }],
        page: 1,
        pageSize: 25,
        totalCount: 1,
        capacityThresholdReached: true,
      },
      isPending: false,
      isError: false,
      isFetching: false,
      error: null,
    } as never);

    render(
      <LocalizationProvider locale="en" messages={securityMessageCatalogs.en}>
        <SecurityAuditReviewPage />
      </LocalizationProvider>,
    );

    expect(screen.getByText("Audit storage has reached its configured warning threshold.")).toBeInTheDocument();
    expect(screen.getByText("authentication.password.login")).toBeInTheDocument();
    expect(screen.getByText("203.0.113.7")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /delete|edit|update/i })).not.toBeInTheDocument();
  });

  it("sends localized filters and sort direction to the server query", () => {
    vi.mocked(useAuditSecurityEventsList).mockReturnValue({
      data: { items: [], page: 1, pageSize: 25, totalCount: 0, capacityThresholdReached: false },
      isPending: false,
      isError: false,
      isFetching: false,
      error: null,
    } as never);

    render(
      <LocalizationProvider locale="en" messages={securityMessageCatalogs.en}>
        <SecurityAuditReviewPage />
      </LocalizationProvider>,
    );

    fireEvent.change(screen.getByLabelText("Actor"), { target: { value: "user-2" } });
    fireEvent.change(screen.getByLabelText("From date"), { target: { value: "2026-09-01" } });
    fireEvent.click(screen.getByRole("button", { name: "Apply filters" }));
    fireEvent.click(screen.getByRole("button", { name: "Descending" }));

    const lastParams = vi.mocked(useAuditSecurityEventsList).mock.lastCall?.[0] as Record<string, unknown>;
    expect(lastParams).toMatchObject({
      actorId: "user-2",
      fromUtc: "2026-09-01T00:00:00.000Z",
      sortBy: 1,
      descending: false,
    });
  });

  it("renders the localized dependency-unavailable state", () => {
    vi.mocked(useAuditSecurityEventsList).mockReturnValue({
      data: undefined,
      isPending: false,
      isError: true,
      isFetching: false,
      error: { status: 503 },
    } as never);

    render(
      <LocalizationProvider locale="en" messages={securityMessageCatalogs.en}>
        <SecurityAuditReviewPage />
      </LocalizationProvider>,
    );

    expect(screen.getByText("Security audit review is temporarily unavailable.")).toBeInTheDocument();
  });
});

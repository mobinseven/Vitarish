import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import { LocalizationProvider } from "@vitarish/localization";
import { securityMessageCatalogs } from "../localization";
import { PlatformSecurityAuditReviewPage } from "../components/PlatformSecurityAuditReviewPage";
import {
  auditPlatformSecurityEventsExport,
  useAuditPlatformSecurityEventCatalogGet,
  useAuditPlatformSecurityEventsList,
} from "../api/generated/audit/audit";

vi.mock("../api/generated/audit/audit", () => ({
  auditPlatformSecurityEventsExport: vi.fn(),
  useAuditPlatformSecurityEventCatalogGet: vi.fn(),
  useAuditPlatformSecurityEventsList: vi.fn(),
}));

afterEach(() => {
  cleanup();
  vi.restoreAllMocks();
});

const event = {
  id: "event-1",
  eventName: "authentication.password.login",
  eventCategory: "authentication",
  operation: "password.login",
  outcome: "succeeded",
  actorKind: "user",
  actorId: "actor-a",
  subjectId: "subject-a",
  tenantId: "tenant-a",
  resource: "authentication",
  occurredAt: "2026-09-14T07:00:00.000Z",
  correlationId: "request-a",
  sourceBoundary: "tests",
  sourceIpKind: "unknown",
  sourceIpAddress: "203.0.113.7",
  redactedReason: "redacted",
};

function renderPage() {
  return render(
    <LocalizationProvider locale="en" messages={securityMessageCatalogs.en}>
      <PlatformSecurityAuditReviewPage tenants={[{ id: "tenant-a", name: "Tenant A" }]} />
    </LocalizationProvider>,
  );
}

beforeEach(() => {
  vi.spyOn(HTMLAnchorElement.prototype, "click").mockImplementation(() => undefined);
  vi.mocked(useAuditPlatformSecurityEventCatalogGet).mockReturnValue({
    data: [
      { category: "authentication", eventName: "authentication.password.login", operation: "password.login", isWildcard: false },
      { category: "function", eventName: "security.endpoint.mutation", operation: "*", isWildcard: true },
    ],
    isLoading: false,
    isError: false,
    error: null,
  } as never);
  vi.mocked(useAuditPlatformSecurityEventsList).mockReturnValue({
    data: { items: [event], page: 1, pageSize: 25, totalCount: 1, capacityThresholdReached: false },
    isPending: false,
    isError: false,
    isFetching: false,
    error: null,
  } as never);
  vi.mocked(auditPlatformSecurityEventsExport).mockResolvedValue(new Blob(["csv"], { type: "text/csv" }));
});

describe("PlatformSecurityAuditReviewPage", () => {
  it("sends the applied actor filter to the global query and downloads the filtered CSV", async () => {
    renderPage();

    fireEvent.change(screen.getByLabelText("Actor ID"), { target: { value: "actor-b" } });
    fireEvent.click(screen.getByRole("button", { name: "Apply filters" }));

    await waitFor(() => expect(vi.mocked(useAuditPlatformSecurityEventsList).mock.lastCall?.[0]).toMatchObject({
      actorId: "actor-b",
      tenantId: undefined,
      uncataloguedOnly: false,
      fromUtc: expect.stringMatching(/T00:00:00\.000Z$/),
      toUtc: expect.stringMatching(/T23:59:59\.999Z$/),
    }));

    fireEvent.click(screen.getByRole("button", { name: "Export CSV" }));
    await waitFor(() => expect(auditPlatformSecurityEventsExport).toHaveBeenCalledWith(expect.objectContaining({ actorId: "actor-b" })));
  });

  it("supports exact legacy event and operation fallbacks with removable active filters", async () => {
    renderPage();

    fireEvent.click(screen.getByLabelText("Uncatalogued only"));
    fireEvent.change(screen.getByLabelText("Exact event type"), { target: { value: "legacy.event" } });
    fireEvent.change(screen.getByLabelText("Exact operation"), { target: { value: "legacy.operation" } });
    fireEvent.click(screen.getByRole("button", { name: "Apply filters" }));

    await waitFor(() => expect(vi.mocked(useAuditPlatformSecurityEventsList).mock.lastCall?.[0]).toMatchObject({
      eventCategory: undefined,
      eventName: "legacy.event",
      operation: "legacy.operation",
      uncataloguedOnly: true,
    }));
    expect(screen.getByRole("button", { name: "Remove filter: Uncatalogued only" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Remove filter: Event type: legacy.event" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Remove filter: Operation: legacy.operation" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "Export CSV" })).toBeEnabled();
    fireEvent.click(screen.getByRole("button", { name: "Export CSV" }));
    await waitFor(() => expect(auditPlatformSecurityEventsExport).toHaveBeenCalledWith(expect.objectContaining({
      uncataloguedOnly: true,
      eventCategory: undefined,
      eventName: "legacy.event",
      operation: "legacy.operation",
    })));

    fireEvent.click(screen.getByRole("button", { name: "Remove filter: Event type: legacy.event" }));
    await waitFor(() => expect(vi.mocked(useAuditPlatformSecurityEventsList).mock.lastCall?.[0]).toMatchObject({
      eventName: undefined,
      operation: undefined,
      uncataloguedOnly: true,
    }));
  });

  it("renders the catalog, operation, source-IP columns and all review-safe detail fields", async () => {
    renderPage();

    expect(screen.getByRole("columnheader", { name: "Catalog category" })).toBeInTheDocument();
    expect(screen.getByRole("columnheader", { name: "Operation" })).toBeInTheDocument();
    expect(screen.getByRole("columnheader", { name: "Source IP" })).toBeInTheDocument();

    const eventCells = screen.getAllByText("authentication.password.login");
    fireEvent.click(eventCells[eventCells.length - 1]);
    await waitFor(() => expect(screen.getByRole("heading", { name: "Event details" })).toBeInTheDocument());
    expect(screen.getByText("event-1")).toBeInTheDocument();
    expect(screen.getByText("subject-a")).toBeInTheDocument();
    expect(screen.getAllByText("203.0.113.7")).toHaveLength(2);
    expect(screen.getByText("redacted")).toBeInTheDocument();
  });

  it("renders capacity warnings and service authorization errors", async () => {
    vi.mocked(useAuditPlatformSecurityEventsList).mockReturnValue({
      data: { items: [], page: 1, pageSize: 25, totalCount: 0, capacityThresholdReached: true },
      isPending: false,
      isError: true,
      isFetching: false,
      error: { status: 403 },
    } as never);
    renderPage();

    expect(screen.getByText("Audit storage has reached its configured warning threshold.")).toBeInTheDocument();
    expect(screen.getByText("Platform administrator access is required for global audit review.")).toBeInTheDocument();
  });

  it("renders loading, empty, and dependency-unavailable states explicitly", () => {
    vi.mocked(useAuditPlatformSecurityEventsList).mockReturnValue({
      data: undefined,
      isPending: true,
      isError: false,
      isFetching: true,
      error: null,
    } as never);
    renderPage();
    expect(screen.getByLabelText("Loading security events")).toBeInTheDocument();

    cleanup();
    vi.mocked(useAuditPlatformSecurityEventsList).mockReturnValue({
      data: { items: [], page: 1, pageSize: 25, totalCount: 0, capacityThresholdReached: false },
      isPending: false,
      isError: false,
      isFetching: false,
      error: null,
    } as never);
    renderPage();
    expect(screen.getByText("No security events match these filters.")).toBeInTheDocument();

    cleanup();
    vi.mocked(useAuditPlatformSecurityEventsList).mockReturnValue({
      data: undefined,
      isPending: false,
      isError: true,
      isFetching: false,
      error: { status: 503 },
    } as never);
    renderPage();
    expect(screen.getByText("Global security audit is temporarily unavailable.")).toBeInTheDocument();
  });

  it("renders an export capacity error without changing the committed list", async () => {
    vi.mocked(auditPlatformSecurityEventsExport).mockRejectedValue({ status: 413 });
    renderPage();

    fireEvent.click(screen.getByRole("button", { name: "Export CSV" }));
    await waitFor(() => expect(screen.getByText("The filtered result exceeds the configured CSV export limit. Narrow the filters and try again.")).toBeInTheDocument());
  });

  it("opens a read-only detail sheet from a selected event row", async () => {
    renderPage();

    const eventCells = screen.getAllByText("authentication.password.login");
    fireEvent.click(eventCells[eventCells.length - 1]);

    await waitFor(() => expect(screen.getByRole("heading", { name: "Event details" })).toBeInTheDocument());
    expect(screen.getByText("request-a")).toBeInTheDocument();
    expect(screen.queryByRole("button", { name: /delete|edit|update/i })).not.toBeInTheDocument();
  });
});

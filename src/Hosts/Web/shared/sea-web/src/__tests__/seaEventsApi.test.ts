import { beforeEach, describe, expect, it, vi } from "vitest";
import { apiFetch } from "../api/client/apiFetch";
import { createSeaEventAfter } from "../features/sea/events/api/seaEventsApi";

vi.mock("../api/client/apiFetch", () => ({
  apiFetch: vi.fn(),
}));

const mockedApiFetch = vi.mocked(apiFetch);

describe("SEA events API", () => {
  beforeEach(() => {
    mockedApiFetch.mockClear();
    mockedApiFetch.mockResolvedValue({ succeeded: true, entityId: "event-1" });
  });

  it("creates the first event through the optional previous-event route", async () => {
    await createSeaEventAfter(undefined, {
      projectId: "project-1",
      procedureId: "procedure-1",
      title: "Initial event",
    });

    expect(mockedApiFetch).toHaveBeenCalledWith(
      expect.objectContaining({
        url: "/api/modules/sea/events/after",
        method: "POST",
        data: {
          projectId: "project-1",
          procedureId: "procedure-1",
          title: "Initial event",
        },
      }),
      undefined
    );
  });

  it("appends an event after the selected previous event", async () => {
    await createSeaEventAfter("event-1", {
      projectId: "project-1",
      procedureId: "procedure-1",
      title: "Follow-up event",
    });

    expect(mockedApiFetch).toHaveBeenCalledWith(
      expect.objectContaining({
        url: "/api/modules/sea/events/after/event-1",
        method: "POST",
        data: {
          projectId: "project-1",
          procedureId: "procedure-1",
          title: "Follow-up event",
        },
      }),
      undefined
    );
  });
});

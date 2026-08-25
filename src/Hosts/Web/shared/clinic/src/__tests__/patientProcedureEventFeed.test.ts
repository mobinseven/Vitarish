import { describe, expect, it } from "vitest";
import { mergeProcedureEventFeed } from "../components/patients/procedures/PatientProcedureEventItemsPanel";
import type { EventItemSummary } from "../models/patients/procedures/PatientProcedureEventItemModels";

function note(id: string, authorId: string, createdAt: string): EventItemSummary {
  return {
    id,
    eventId: "event-1",
    type: "note",
    content: id,
    authorId,
    createdAt,
  };
}

describe("patient procedure event feed", () => {
  it("merges typed items chronologically with a stable ID tie-breaker", () => {
    const feed = mergeProcedureEventFeed(
      [
        note("later", "author-1", "2026-07-28T10:02:00Z"),
        note("same-time", "author-1", "2026-07-28T10:01:00Z"),
      ],
      [
        {
          id: "prescription-1",
          pluginId: "medical.prescriptions",
          createdAt: "2026-07-28T10:01:00Z",
          payload: {},
        },
      ],
    );

    expect(feed.map((item) => item.id)).toEqual([
      "medical.prescriptions:prescription-1",
      "sea:same-time",
      "sea:later",
    ]);
  });
});

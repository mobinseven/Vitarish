import { describe, expect, it } from "vitest";
import { appendPrescriptionEditItem } from "../components/patients/procedures/PrescriptionEventFeedItem";

describe("prescription amendment items", () => {
  it("adds a Tamin reference once with editable defaults", () => {
    const result = {
      category: 0,
      referenceCode: "drug-1",
      displayName: "Drug one",
    };
    const added = appendPrescriptionEditItem([], result, "item-1");

    expect(added).toEqual([
      expect.objectContaining({
        id: "item-1",
        referenceCode: "drug-1",
        quantity: 1,
        durationDays: null,
        sortOrder: 1,
      }),
    ]);
    expect(appendPrescriptionEditItem(added, result, "item-2")).toBe(added);
  });
});

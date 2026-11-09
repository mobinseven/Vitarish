import { describe, expect, it } from "vitest";
import {
  buildDrugConsumptionMetadata,
  drugConsumptionMetadataKeys,
} from "../models/prescriptions/drugConsumption";

describe("buildDrugConsumptionMetadata", () => {
  it("emits the keys and numeric ids the provider adapter reads", () => {
    const metadata = JSON.parse(
      buildDrugConsumptionMetadata({
        amountId: "580",
        instructionId: "2546009",
        routeCode: "231271000",
        methodId: "12",
      }),
    );

    expect(metadata).toEqual({
      [drugConsumptionMetadataKeys.amountId]: 580,
      [drugConsumptionMetadataKeys.instructionId]: 2546009,
      [drugConsumptionMetadataKeys.routeCode]: "231271000",
      [drugConsumptionMetadataKeys.methodId]: 12,
    });
    expect(Object.keys(metadata).sort()).toEqual([
      "doseCode",
      "drugAmntId",
      "drugInstId",
      "methodUsageId",
    ]);
  });

  it("omits the optional route and method when they were not chosen", () => {
    const metadata = JSON.parse(
      buildDrugConsumptionMetadata({ amountId: "7", instructionId: "9" }),
    );

    expect(metadata).toEqual({ drugAmntId: 7, drugInstId: 9 });
  });
});

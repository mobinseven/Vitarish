import { describe, expect, it } from "vitest";
import { patientRecordTabs, toPatientRecordTab } from "../components/patients/record/patientRecordTabs";

describe("patient record tabs", () => {
  it("opens the overview unless the address names another tab", () => {
    expect(toPatientRecordTab(null)).toBe("overview");
    expect(toPatientRecordTab(undefined)).toBe("overview");
    expect(toPatientRecordTab("unknown")).toBe("overview");
    for (const tab of patientRecordTabs) expect(toPatientRecordTab(tab)).toBe(tab);
  });
});

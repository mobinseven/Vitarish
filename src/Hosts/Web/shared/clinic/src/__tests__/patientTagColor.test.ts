import { describe, expect, it } from "vitest";
import { chromaChipClassName, chromaColors } from "@vitarish/ui";
import { patientTagClassName } from "../components/patients/tags/patientTagColor";

describe("patientTagClassName", () => {
  it("gives a tag one chroma color regardless of case or surrounding spaces", () => {
    const className = patientTagClassName("Diabetes");

    expect(chromaColors.map(chromaChipClassName)).toContain(className);
    expect(patientTagClassName("  diabetes ")).toBe(className);
  });

  it("spreads different tags across more than one color", () => {
    const classNames = new Set(
      ["VIP", "Diabetes", "Follow-up", "Allergy", "ویژه", "دیابت"].map(patientTagClassName),
    );

    expect(classNames.size).toBeGreaterThan(1);
  });
});

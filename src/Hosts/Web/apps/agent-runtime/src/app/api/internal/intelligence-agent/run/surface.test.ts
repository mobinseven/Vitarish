import { readFileSync } from "node:fs";
import { join } from "node:path";
import { describe, expect, it } from "vitest";
import { surfaceProblem } from "./surface";

// The one Intelligence-owned catalog; Main sends this document with the present_surface tool.
const catalog = readFileSync(join(__dirname, "../../../../../../../../shared/intelligence/src/surfaces/assistant-catalog.json"), "utf8");

describe("surfaceProblem", () => {
  it("accepts a surface that binds results to handles", () => {
    expect(surfaceProblem(catalog, {
      surface: {
        components: [
          { id: "root", component: "Column", children: ["title", "list"] },
          { id: "title", component: "Heading", text: "Patients" },
          { id: "list", component: "RecordList", result: { path: "/r1" }, fields: ["dateOfBirth"] },
        ],
      },
    })).toBeNull();
  });

  it("rejects a typed record value where a data binding is required", () => {
    expect(surfaceProblem(catalog, {
      surface: { components: [{ id: "root", component: "RecordCard", result: "Sara Ahmadi", fields: [] }] },
    })).toMatch(/not valid/);
  });

  it("rejects a component outside the catalog", () => {
    expect(surfaceProblem(catalog, {
      surface: { components: [{ id: "root", component: "Button", text: "Delete" }] },
    })).toMatch(/not valid/);
  });

  it("rejects record values smuggled into extra properties", () => {
    expect(surfaceProblem(catalog, {
      surface: { components: [{ id: "root", component: "Heading", text: "Patient", value: "Sara Ahmadi" }] },
    })).toMatch(/not valid/);
  });

  it("rejects a surface without components", () => {
    expect(surfaceProblem(catalog, { surface: {} })).toMatch(/components/);
  });
});

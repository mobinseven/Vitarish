import { describe, expect, it } from "vitest";
import { tool } from "@openai/agents";
import { adaptToolParameters } from "./tool-parameters";
import { readFileSync } from "node:fs";
import { resolve } from "node:path";

const schema = {
  type: "object",
  properties: { operation: { type: "string" }, payload: { type: "object" }, commit: { type: "boolean" } },
  required: ["operation"],
  additionalProperties: false,
};

describe("strict provider tool parameters", () => {
  it("adapts the actual Search dictionary schema and restores nested arguments", () => {
    const source = readFileSync(resolve(process.cwd(), "../../../../Modules/Search/Agents/SearchAgentTool.cs"), "utf8");
    const searchSchema = JSON.parse(source.match(/"""([\s\S]*?)"""/)![1]);
    const adapted = adaptToolParameters(searchSchema);
    const definition = tool({ name: "diagnostic", description: "Schema probe", parameters: adapted.parameters as never,
      strict: true, execute: async () => "" });
    expect(definition.parameters).toMatchObject({ properties: { filters: { anyOf: [{
      properties: { metadataFilters: { anyOf: [{ type: "string" }, { type: "null" }] } },
    }, { type: "null" }] } } });
    expect(adapted.decodeArguments({ query: "example", filters: { type: null, corpusId: null, metadataFilters: '{"category":"example"}' },
      limit: null, registeredIndex: null, retrievalMode: null })).toEqual({ query: "example", filters: { metadataFilters: { category: "example" } } });
    expect(() => adapted.decodeArguments({ query: "example", filters: { metadataFilters: "[]" } })).toThrow();
  });

  it("decodes dictionaries in array items and retains explicit nullable arguments", () => {
    const adapted = adaptToolParameters({ type: "object", properties: {
      entries: { type: "array", items: { type: "object", properties: { data: { type: "object" } }, additionalProperties: false } },
      note: { type: ["string", "null"] },
    }, additionalProperties: false });
    expect(adapted.decodeArguments({ entries: [{ data: '{"enabled":true}' }], note: null }))
      .toEqual({ entries: [{ data: { enabled: true } }], note: null });
  });
  it("makes parameterless tools explicit empty objects for provider validation", () => {
    const adapted = adaptToolParameters({ type: "object", additionalProperties: false });
    const definition = tool({ name: "diagnostic", description: "Schema probe", parameters: adapted.parameters as never,
      strict: true, execute: async () => "" });
    expect(definition.parameters).toEqual({ type: "object", properties: {}, required: [], additionalProperties: false });
    expect(adapted.decodeArguments({})).toEqual({});
  });
  it("represents arbitrary payloads in a strict SDK schema and restores the application object", () => {
    const adapted = adaptToolParameters(schema);
    const definition = tool({ name: "diagnostic", description: "Schema probe", parameters: adapted.parameters as never,
      strict: true, execute: async () => "" });
    expect(definition.parameters).toMatchObject({
      properties: { payload: { anyOf: [{ type: "string" }, { type: "null" }] } },
      required: ["operation", "payload", "commit"], additionalProperties: false,
    });
    const payload = { name: "Example", nested: { enabled: true }, items: [1, 2] };
    expect(adapted.decodeArguments({ operation: "example", payload: JSON.stringify(payload), commit: false }))
      .toEqual({ operation: "example", payload, commit: false });
    expect(schema.properties.payload).toEqual({ type: "object" });
  });

  it.each(["invalid JSON", "null", "[]", "42", '"text"', {}])("rejects invalid object payload %j before callback", (payload) => {
    expect(() => adaptToolParameters(schema).decodeArguments({ operation: "example", payload })).toThrow();
  });

  it("restores omitted optional payloads instead of passing the SDK's null into application code", () => {
    expect(adaptToolParameters(schema).decodeArguments({ operation: "example", payload: null })).toEqual({ operation: "example" });
  });

  it("does not allow null for a required object payload", () => {
    const required = { ...schema, required: ["operation", "payload"] };
    expect(() => adaptToolParameters(required).decodeArguments({ operation: "example", payload: null })).toThrow();
  });

  it("leaves structured object schemas and their arguments intact", () => {
    const structured = { type: "object", properties: { item: { type: "object", properties: { name: { type: "string" } }, additionalProperties: false } } };
    const adapted = adaptToolParameters(structured);
    expect(adapted.parameters).toEqual(structured);
    const value = { item: { name: "Example" } };
    expect(adapted.decodeArguments(value)).toBe(value);
  });
});

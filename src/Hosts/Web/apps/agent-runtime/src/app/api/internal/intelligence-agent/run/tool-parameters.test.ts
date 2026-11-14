import { describe, expect, it } from "vitest";
import { tool } from "@openai/agents";
import { adaptToolParameters } from "./tool-parameters";

const schema = {
  type: "object",
  properties: { operation: { type: "string" }, payload: { type: "object" }, commit: { type: "boolean" } },
  required: ["operation"],
  additionalProperties: false,
};

describe("strict provider tool parameters", () => {
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

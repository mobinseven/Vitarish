function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function allowsNull(schema: unknown): boolean {
  return isRecord(schema) && (schema.type === "null" ||
    (Array.isArray(schema.type) && schema.type.includes("null")) ||
    (Array.isArray(schema.anyOf) && schema.anyOf.some(allowsNull)) ||
    (Array.isArray(schema.oneOf) && schema.oneOf.some(allowsNull)));
}

interface ToolParameters {
  parameters: unknown;
  decodeArguments(value: unknown): unknown;
}

/** Adapt provider schemas recursively; callbacks retain their source-owned object contracts and validation. */
export function adaptToolParameters(schema: unknown): ToolParameters {
  const unchanged = { parameters: schema, decodeArguments: (value: unknown) => value };
  if (!isRecord(schema)) return unchanged;
  if (schema.type === "array" && schema.items) {
    const items = adaptToolParameters(schema.items);
    return {
      parameters: { ...schema, items: items.parameters },
      decodeArguments: (value) => Array.isArray(value) ? value.map(items.decodeArguments) : value,
    };
  }
  if (schema.type !== "object") return unchanged;
  if (schema.properties === undefined && schema.additionalProperties !== false) {
    // Strict schemas cannot represent dictionaries. The callback retains validation of their contents.
    return {
      parameters: {
        type: "string",
        description: `A JSON-encoded object. It must satisfy this schema: ${JSON.stringify(schema)}`,
      },
      decodeArguments(value) {
        if (typeof value !== "string") throw new Error("Tool object arguments must be JSON text.");
        const object: unknown = JSON.parse(value);
        if (!isRecord(object)) throw new Error("Tool JSON arguments must decode to an object.");
        return object;
      },
    };
  }
  const properties = isRecord(schema.properties) ? schema.properties : {};
  const children = Object.fromEntries(Object.entries(properties).map(([key, property]) => [key, adaptToolParameters(property)]));
  const required = Array.isArray(schema.required) ? schema.required : [];
  return {
    parameters: {
      ...schema,
      properties: Object.fromEntries(Object.entries(children).map(([key, child]) => [key, child.parameters])),
      ...(schema.properties === undefined ? { required } : {}),
    },
    decodeArguments(value) {
      if (!isRecord(value)) throw new Error("Tool arguments must be an object.");
      const decoded = { ...value };
      let changed = false;
      for (const [key, child] of Object.entries(children)) {
        if (!(key in value)) continue;
        // Remove only nulls injected by the SDK for originally optional, non-nullable properties.
        if (value[key] === null && !required.includes(key) && !allowsNull(properties[key])) {
          delete decoded[key];
          changed = true;
          continue;
        }
        if (value[key] === null && allowsNull(properties[key])) continue;
        decoded[key] = child.decodeArguments(value[key]);
        changed ||= decoded[key] !== value[key];
      }
      return changed ? decoded : value;
    },
  };
}

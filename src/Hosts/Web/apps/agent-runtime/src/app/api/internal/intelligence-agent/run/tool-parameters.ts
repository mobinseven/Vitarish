function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

/** Keep free-form application objects out of strict provider schemas without changing callback arguments. */
export function adaptToolParameters(schema: unknown) {
  if (isRecord(schema) && schema.type === "object" && schema.properties === undefined && schema.additionalProperties === false) {
    return { parameters: { ...schema, properties: {}, required: schema.required ?? [] }, decodeArguments: (value: unknown) => value };
  }
  if (!isRecord(schema) || !isRecord(schema.properties)) {
    return { parameters: schema, decodeArguments: (value: unknown) => value };
  }

  const properties = { ...schema.properties };
  const encodedProperties: string[] = [];
  const required = Array.isArray(schema.required) ? schema.required : [];
  for (const [key, property] of Object.entries(properties)) {
    if (!isRecord(property) || property.type !== "object" || property.properties !== undefined ||
        property.additionalProperties === false) continue;
    // Strict schemas cannot represent an object with arbitrary keys. JSON text preserves that contract.
    properties[key] = {
      type: "string",
      description: [property.description, "A JSON-encoded object containing the operation's arguments."].filter(Boolean).join(" "),
    };
    encodedProperties.push(key);
  }

  return {
    parameters: { ...schema, properties },
    decodeArguments(value: unknown): unknown {
      if (encodedProperties.length === 0) return value;
      if (!isRecord(value)) throw new Error("Tool arguments must be an object.");
      const decoded = { ...value };
      for (const key of encodedProperties) {
        // The SDK represents omitted optional properties as null in its strict wire schema.
        if ((decoded[key] === undefined || decoded[key] === null) && !required.includes(key)) {
          delete decoded[key];
          continue;
        }
        if (typeof decoded[key] !== "string") throw new Error("Tool object arguments must be JSON text.");
        const object: unknown = JSON.parse(decoded[key]);
        if (!isRecord(object)) throw new Error("Tool JSON arguments must decode to an object.");
        decoded[key] = object;
      }
      return decoded;
    },
  };
}

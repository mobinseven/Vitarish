import { describe, expect, it } from "vitest";
import { enMessages } from "@/localization/messages/en";
import { faMessages } from "@/localization/messages/fa";

function collectLeafKeys(value: unknown, path = ""): string[] {
  if (typeof value === "string") return [path];
  if (!value || typeof value !== "object") return [];

  return Object.entries(value).flatMap(([key, child]) =>
    collectLeafKeys(child, path ? `${path}.${key}` : key)
  );
}

describe("SEA localization messages", () => {
  it("keeps new React SEA journey catalog keys in English and Persian", () => {
    const requiredKeys = collectLeafKeys(faMessages.sea);
    const englishKeys = new Set(collectLeafKeys(enMessages.sea));

    for (const key of requiredKeys) {
      expect(englishKeys.has(key), key).toBe(true);
    }
  });
});

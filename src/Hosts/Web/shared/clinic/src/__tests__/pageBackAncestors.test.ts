import { describe, expect, it } from "vitest";
import { pageAncestors } from "../components/navigation/usePageBack";
import { clinicEnMessages } from "../localization/en";
import { clinicFaMessages } from "../localization/fa";

function read(messages: unknown, key: string): unknown {
  return key.split(".").reduce<unknown>((node, part) => (node && typeof node === "object" ? (node as Record<string, unknown>)[part] : undefined), messages);
}

describe("page back ancestors", () => {
  it.each(Object.entries(pageAncestors))("names %s in both languages", (_name, ancestor) => {
    for (const catalog of [clinicEnMessages, clinicFaMessages]) {
      expect(typeof read(catalog, ancestor.labelKey), ancestor.labelKey).toBe("string");
    }
  });

  it("has the back-trail messages in both languages", () => {
    for (const catalog of [clinicEnMessages, clinicFaMessages]) {
      expect(read(catalog, "clinic.navigation.pageLocation")).toBeTypeOf("string");
      expect(read(catalog, "clinic.navigation.backTo")).toContain("{name}");
    }
  });
});

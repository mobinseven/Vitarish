import { describe, expect, it } from "vitest";
import { getApiErrorMessageKey, localizeApiError } from "@/localization";

describe("localized API errors", () => {
  it("maps known API codes", () => {
    expect(getApiErrorMessageKey({ code: "Unauthorized" })).toBe("errors.unauthorized");
  });

  it("uses generic fallback for unknown codes", () => {
    expect(getApiErrorMessageKey({ code: "Unexpected" })).toBe("errors.generic");
  });

  it("uses generic fallback for thrown errors", () => {
    expect(localizeApiError(new Error("boom"), (key) => key)).toBe("errors.generic");
  });
});

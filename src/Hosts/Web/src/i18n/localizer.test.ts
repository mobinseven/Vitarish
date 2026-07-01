import { describe, expect, it } from "vitest";
import { normalizeCulture } from "./cultures";
import { createLocalizer, translate } from "./localizer";


describe("React web localizer", () => {
  it("normalizes supported culture aliases", () => {
    expect(normalizeCulture("en")).toBe("en-US");
    expect(normalizeCulture("fa")).toBe("fa-IR");
    expect(normalizeCulture("unknown")).toBe("en-US");
  });

  it("translates known keys for the active culture", () => {
    expect(translate("fa-IR", "Common.Save")).toBe("ذخیره");
  });

  it("formats message values without string concatenation", () => {
    const localizer = createLocalizer("en-US");

    expect(localizer.t("Account.SignedInAs", { name: "Farzad" })).toBe(
      "Signed in as Farzad",
    );
  });

  it("sets direction from culture", () => {
    expect(createLocalizer("fa-IR").direction).toBe("rtl");
    expect(createLocalizer("en-US").direction).toBe("ltr");
  });
});

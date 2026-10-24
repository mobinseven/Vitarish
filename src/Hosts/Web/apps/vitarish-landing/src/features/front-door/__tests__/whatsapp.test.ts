import { describe, expect, it } from "vitest";
import { readWhatsAppNumber, whatsappChatUrl, whatsappNumberEnvironmentKey } from "../whatsapp";

describe("readWhatsAppNumber", () => {
  it("accepts an international number with or without a leading plus", () => {
    expect(readWhatsAppNumber({ [whatsappNumberEnvironmentKey]: " +989121234567 " })).toBe("989121234567");
    expect(readWhatsAppNumber({ [whatsappNumberEnvironmentKey]: "989121234567" })).toBe("989121234567");
  });

  it("offers no number when it is unset or malformed", () => {
    expect(readWhatsAppNumber({})).toBeNull();
    expect(readWhatsAppNumber({ [whatsappNumberEnvironmentKey]: "0912 123 4567" })).toBeNull();
    expect(readWhatsAppNumber({ [whatsappNumberEnvironmentKey]: "1234567" })).toBeNull();
  });
});

describe("whatsappChatUrl", () => {
  it("encodes the message so the visitor sees it unchanged", () => {
    expect(whatsappChatUrl("989121234567", "سلام ویتاریش & co?")).toBe(
      `https://wa.me/989121234567?text=${encodeURIComponent("سلام ویتاریش & co?")}`,
    );
  });
});

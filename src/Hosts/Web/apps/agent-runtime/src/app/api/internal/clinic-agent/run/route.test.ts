import { describe, expect, it } from "vitest";
import { parseHistory } from "./route";

describe("clinic agent history", () => {
  it("accepts supported SDK history items", () => {
    const history = JSON.stringify([
      { role: "user", content: "hello" },
      {
        role: "assistant",
        status: "completed",
        content: [{ type: "output_text", text: "hi" }],
      },
    ]);

    expect(parseHistory(history)).toHaveLength(2);
  });

  it.each([
    "{}",
    '[{"role":"owner","content":"nope"}]',
    '[{"type":"unsupported"}]',
    "not-json",
  ])("rejects malformed or unsupported history: %s", (history) => {
    expect(() => parseHistory(history)).toThrow("InvalidHistory");
  });

  it("rejects history over the plaintext limit", () => {
    const oversized = JSON.stringify([{ role: "user", content: "x".repeat(1024 * 1024) }]);

    expect(() => parseHistory(oversized)).toThrow("InvalidHistory");
  });
});

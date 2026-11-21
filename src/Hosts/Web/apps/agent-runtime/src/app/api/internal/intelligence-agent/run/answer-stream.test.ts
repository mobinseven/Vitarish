import { describe, expect, it } from "vitest";
import { AnswerFieldDecoder, isValidAnswerStreamRequest, splitAnswerText } from "./answer-stream";
import { MAX_ANSWER_DELTA_CHARS, MAX_ANSWER_STREAM_CHARS } from "./stream-frames";

const path = ["answer"];

function decodeChunks(chunks: string[], fieldPath: string[] = path) {
  const decoder = new AnswerFieldDecoder(fieldPath);
  return { text: chunks.map((chunk) => decoder.push(chunk)).join(""), decoder };
}
function decodeWhole(json: string, fieldPath?: string[]) {
  return decodeChunks([json], fieldPath);
}
/** Every possible split of the text into two and into single characters must decode identically. */
function expectSameForEverySplit(json: string, expected: string, fieldPath?: string[]) {
  expect(decodeWhole(json, fieldPath).text).toBe(expected);
  expect(decodeChunks([...json], fieldPath).text).toBe(expected);
  for (let cut = 1; cut < json.length; cut += 1) {
    expect(decodeChunks([json.slice(0, cut), json.slice(cut)], fieldPath).text).toBe(expected);
  }
}

describe("answer field decoder", () => {
  it("decodes the designated field across any chunk boundary and ignores every other field", () => {
    expectSameForEverySplit('{"reasoning":"secret","answer":"Hello there","other":{"answer":"nested"}}', "Hello there");
  });

  it("decodes escaped quotes, backslashes, slashes and control escapes split across chunks", () => {
    const json = String.raw`{"answer":"say \"hi\" \\ a\/b\n\t\u0041"}`;
    expectSameForEverySplit(json, 'say "hi" ' + String.fromCharCode(92) + ' a/b' + String.fromCharCode(10, 9) + 'A');
  });

  it("decodes Persian text, raw and escaped, across chunk boundaries", () => {
    expectSameForEverySplit('{"answer":"سلام دنیا \\u0633\\u0644\\u0627\\u0645"}', "سلام دنیا سلام");
  });

  it("joins surrogate pairs given raw or as escapes, including when split between chunks", () => {
    expectSameForEverySplit('{"answer":"a😀b"}', "a😀b");
    expectSameForEverySplit('{"answer":"a\\ud83d\\ude00b"}', "a😀b");
    const decoder = new AnswerFieldDecoder(path);
    const first = decoder.push('{"answer":"x\ud83d');
    expect(first).toBe("x");
    expect(decoder.push('\ude00y"}')).toBe("😀y");
  });

  it("never emits a lone surrogate and stops on unpaired ones", () => {
    for (const bad of ['{"answer":"a\\ud83dz"}', '{"answer":"a\\ude00z"}', '{"answer":"a\\ud83d"}']) {
      const { text, decoder } = decodeWhole(bad);
      expect(text).not.toMatch(/[\ud800-\udfff]/);
      expect(decoder.stopped).toBe(true);
    }
  });

  it("follows an object path and ignores a same-named field elsewhere", () => {
    const json = '{"meta":{"answer":"no"},"result":{"answer":"yes"},"answer":"no"}';
    expectSameForEverySplit(json, "yes", ["result", "answer"]);
  });

  it("decodes the field regardless of field order and skips scalars, arrays and nested objects before it", () => {
    expectSameForEverySplit('{"n":-1.5e3,"ok":true,"z":null,"list":[1,"x",{"answer":"no"}],"answer":"late"}', "late");
  });

  it("stops after the field completes and ignores a duplicate or trailing content", () => {
    const { text, decoder } = decodeWhole('{"answer":"first","answer":"second"');
    expect(text).toBe("first");
    expect(decoder.stopped).toBe(true);
  });

  it("stops without emitting on a wrong-typed field or malformed JSON", () => {
    for (const bad of [
      '{"answer":5}', '{"answer":null}', '{"answer":{"x":"y"}}', '{"answer":["y"]}',
      '["answer"]', 'answer', '{"answer" "x"}', '{answer:"x"}', '{"answer":"bad \\q escape"}',
      '{"answer":"bad \\u00zz"}', '{"answer":"raw\ncontrol"}', '{"a":1,,"answer":"x"}', '{"a":1}}{"answer":"x"}',
    ]) {
      const { text, decoder } = decodeWhole(bad);
      expect(text, bad).toBe("");
      expect(decoder.stopped, bad).toBe(true);
    }
  });

  it("releases nothing from a chunk that turns out to be malformed, and nothing afterwards", () => {
    const decoder = new AnswerFieldDecoder(path);
    expect(decoder.push('{"answer":"ok')).toBe("ok");
    expect(decoder.push('\\qmore')).toBe("");
    expect(decoder.stopped).toBe(true);
    expect(decoder.push('"}')).toBe("");
  });

  it("emits nothing when the field never appears", () => {
    const { text } = decodeWhole('{"reasoning":"x","other":"y"}');
    expect(text).toBe("");
  });

  it("bounds the total streamed text", () => {
    const decoder = new AnswerFieldDecoder(path);
    const total = decoder.push(`{"answer":"${"a".repeat(MAX_ANSWER_STREAM_CHARS + 500)}"}`);
    expect(total).toHaveLength(MAX_ANSWER_STREAM_CHARS);
    expect(decoder.stopped).toBe(true);
  });

  it("splits long text into bounded pieces without separating a surrogate pair", () => {
    const text = `${"a".repeat(MAX_ANSWER_DELTA_CHARS - 1)}😀${"b".repeat(10)}`;
    const pieces = splitAnswerText(text);
    expect(pieces.join("")).toBe(text);
    for (const piece of pieces) {
      expect(piece.length).toBeLessThanOrEqual(MAX_ANSWER_DELTA_CHARS);
      expect(piece).not.toMatch(/^[\udc00-\udfff]|[\ud800-\udbff]$/);
    }
    expect(splitAnswerText("")).toEqual([]);
  });

  it("validates the run-request declaration", () => {
    expect(isValidAnswerStreamRequest({ messageId: "m1", fieldPath: ["answer"] })).toBe(true);
    for (const bad of [null, [], {}, { messageId: " ", fieldPath: ["a"] }, { messageId: "m", fieldPath: [] },
      { messageId: "m", fieldPath: [""] }, { messageId: "m", fieldPath: [1] }, { messageId: "m", fieldPath: ["a", "b", "c", "d", "e"] },
      { messageId: "m".repeat(129), fieldPath: ["a"] }]) {
      expect(isValidAnswerStreamRequest(bad)).toBe(false);
    }
  });
});

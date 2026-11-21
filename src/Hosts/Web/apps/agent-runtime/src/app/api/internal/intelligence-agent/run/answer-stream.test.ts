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

  it("keeps validating after the field completes: a repeated answer key or trailing content is malformed", () => {
    const duplicate = decodeChunks(['{"answer":"first",', '"answer":"second"}']);
    expect(duplicate.text).toBe("first");
    expect(duplicate.decoder.stopped).toBe(true);
    expect(duplicate.decoder.wellFormed).toBe(false);
    expect(decodeWhole('{"answer":"first","answer":"second"}').text).toBe("");

    const nested = decodeChunks(['{"result":{"answer":"first"},', '"result":{"answer":"second"}}'], ["result", "answer"]);
    expect(nested.decoder.wellFormed).toBe(false);

    const trailing = decodeChunks(['{"answer":"first"}', '{"answer":"second"}']);
    expect(trailing.text).toBe("first");
    expect(trailing.decoder.wellFormed).toBe(false);

    const sameNameElsewhere = decodeWhole('{"other":{"answer":"a"},"answer":"first","list":[{"answer":"b"},{"answer":"c"}]}');
    expect(sameNameElsewhere.text).toBe("first");
    expect(sameNameElsewhere.decoder.wellFormed).toBe(true);
  });

  it("is well formed only for one complete object", () => {
    expect(decodeWhole('{"answer":"x"}').decoder.wellFormed).toBe(true);
    expect(decodeWhole('{"answer":"x"').decoder.wellFormed).toBe(false);
    expect(decodeWhole('{"answer":"x","n":1').decoder.wellFormed).toBe(false);
  });

  it("rejects malformed skipped scalars before any later answer text is emitted", () => {
    for (const bad of [
      '{"other":garbage,"answer":"visible"}', '{"other":01,"answer":"visible"}', '{"other":tru,"answer":"visible"}',
      '{"other":nul,"answer":"visible"}', '{"other":1.,"answer":"visible"}', '{"other":-,"answer":"visible"}',
      '{"other":1e,"answer":"visible"}', '{"other":+1,"answer":"visible"}', '{"other":truee,"answer":"visible"}',
      '{"other":"x","list":[0x1],"answer":"visible"}', '{"other":.5,"answer":"visible"}',
    ]) {
      for (const chunks of [[bad], [...bad]]) {
        const { text, decoder } = decodeChunks(chunks);
        expect(text, bad).toBe("");
        expect(decoder.stopped, bad).toBe(true);
        expect(decoder.wellFormed, bad).toBe(false);
      }
    }
    expectSameForEverySplit('{"a":0,"b":-0.5,"c":1E+2,"d":12.50e-3,"e":false,"f":[true,null],"answer":"ok"}', "ok");
  });

  it("accepts valid numbers of any length and still rejects an invalid long one", () => {
    const long = `0.${"0".repeat(500)}1`;
    const huge = `${"9".repeat(300)}e-${"0".repeat(100)}5`;
    for (const value of [long, huge, `-${long}`, `1${"0".repeat(400)}`]) {
      const json = `{"answer":"ok","n":${value},"after":[${value}]}`;
      expect(JSON.parse(json).n).toBeDefined();
      expectSameForEverySplit(json, "ok");
      expect(decodeWhole(json).decoder.wellFormed).toBe(true);
    }
    for (const bad of [`01${"0".repeat(100)}`, `1.${"0".repeat(100)}.5`, `1${"0".repeat(100)}e`, `1${"0".repeat(100)}x`]) {
      const decoder = decodeWhole(`{"n":${bad},"answer":"visible"}`).decoder;
      expect(decoder.stopped, bad).toBe(true);
      expect(decoder.wellFormed, bad).toBe(false);
    }
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
    expect(decoder.stopped).toBe(false);
    expect(decoder.wellFormed).toBe(true);
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

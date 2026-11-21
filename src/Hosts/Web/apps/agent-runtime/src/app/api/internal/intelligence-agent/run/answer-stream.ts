/**
 * Incremental decoder for the user-visible answer string of a structured (JSON) model output, and the per-run
 * emitter built on it. Eligibility is declared by the caller in the run request; nothing here infers it from
 * model output. Only the designated string field is ever decoded: other fields, reasoning and tool data are
 * skipped and never leave the runtime. Any anomaly stops emission for the rest of the run; the terminal result
 * is produced from the complete model output exactly as in a buffered run.
 */
import { MAX_ANSWER_DELTA_CHARS, MAX_ANSWER_STREAM_CHARS } from "./stream-frames";

/** Run-request declaration that the answer string at `fieldPath` (object keys from the root) may be streamed. */
export interface AnswerStreamRequest {
  messageId: string;
  fieldPath: string[];
}

const MAX_MESSAGE_ID_LENGTH = 128;
const MAX_FIELD_PATH_DEPTH = 4;
const MAX_FIELD_NAME_LENGTH = 128;

export function isValidAnswerStreamRequest(value: unknown): value is AnswerStreamRequest {
  if (typeof value !== "object" || value === null || Array.isArray(value)) return false;
  const { messageId, fieldPath } = value as Partial<AnswerStreamRequest>;
  return typeof messageId === "string" && messageId.trim().length > 0 && messageId.length <= MAX_MESSAGE_ID_LENGTH &&
    Array.isArray(fieldPath) && fieldPath.length >= 1 && fieldPath.length <= MAX_FIELD_PATH_DEPTH &&
    fieldPath.every((name) => typeof name === "string" && name.length > 0 && name.length <= MAX_FIELD_NAME_LENGTH);
}

const MAX_KEY_LENGTH = MAX_FIELD_NAME_LENGTH;

type Phase = "key" | "colon" | "value" | "comma";
interface Container {
  isObject: boolean;
  phase: Phase;
  /** Object only: the key whose value is current; null when unset or longer than any designated name. */
  key: string | null;
  /** True until the first member, so that `{}` and `[]` close without a preceding value. */
  empty: boolean;
}
type StringRole = "key" | "target" | "other";
const simpleEscapes: Record<string, string> = { '"': '"', "\\": "\\", "/": "/", b: "\b", f: "\f", n: "\n", r: "\r", t: "\t" };
const isHigh = (unit: number) => unit >= 0xd800 && unit <= 0xdbff;
const isLow = (unit: number) => unit >= 0xdc00 && unit <= 0xdfff;
const isScalarChar = (char: string) => /[0-9A-Za-z+\-.]/.test(char);

export class AnswerFieldDecoder {
  private readonly stack: Container[] = [];
  private readonly out: string[] = [];
  private inString: StringRole | null = null;
  private escape: "none" | "slash" | string = "none";
  private keyBuffer = "";
  private keyOverflow = false;
  private pendingHigh: number | null = null;
  private inScalar = false;
  private rootClosed = false;
  private emitted = 0;
  private halted = false;
  private anomaly = false;

  constructor(private readonly fieldPath: readonly string[]) {}

  /** True once the field completed, the output was malformed or the size bound was reached. */
  get stopped(): boolean {
    return this.halted;
  }

  /** Feeds the next model text delta; returns the answer text newly decoded from it (possibly empty). */
  push(text: string): string {
    for (let index = 0; index < text.length && !this.halted; index += 1) this.consume(text[index]);
    // Text decoded from a chunk that then proved malformed is not released.
    if (this.anomaly) this.out.length = 0;
    const decoded = this.out.join("");
    this.out.length = 0;
    return decoded;
  }

  private fail(): void {
    this.halted = true;
    this.anomaly = true;
  }

  private consume(char: string): void {
    if (this.inString !== null) {
      this.consumeStringChar(char);
      return;
    }
    if (this.inScalar) {
      if (isScalarChar(char)) return;
      this.inScalar = false;
      this.completeValue();
    }
    if (char === " " || char === "\n" || char === "\r" || char === "\t") return;
    if (this.rootClosed) return this.fail();
    const top = this.stack[this.stack.length - 1];
    if (!top) {
      if (char !== "{") return this.fail();
      this.stack.push({ isObject: true, phase: "key", key: null, empty: true });
      return;
    }
    switch (char) {
      case '"':
        if (top.isObject && top.phase === "key") {
          this.beginString("key");
        } else if (top.phase === "value") {
          this.beginString(this.atTarget() ? "target" : "other");
        } else this.fail();
        return;
      case "{":
      case "[":
        if (top.phase !== "value" || this.atTarget()) return this.fail();
        top.empty = false;
        this.stack.push({ isObject: char === "{", phase: char === "{" ? "key" : "value", key: null, empty: true });
        return;
      case "}":
      case "]": {
        const closesObject = char === "}";
        if (top.isObject !== closesObject || !(top.phase === "comma" || (top.empty && (closesObject ? top.phase === "key" : top.phase === "value")))) return this.fail();
        this.stack.pop();
        if (this.stack.length === 0) this.rootClosed = true;
        else this.completeValue();
        return;
      }
      case ":":
        if (!top.isObject || top.phase !== "colon") return this.fail();
        top.phase = "value";
        return;
      case ",":
        if (top.phase !== "comma") return this.fail();
        top.phase = top.isObject ? "key" : "value";
        return;
      default:
        // Numbers, true, false and null: skipped; a scalar at the designated field is the wrong type.
        if (top.phase !== "value" || !isScalarChar(char) || this.atTarget()) return this.fail();
        top.empty = false;
        this.inScalar = true;
    }
  }

  private completeValue(): void {
    const top = this.stack[this.stack.length - 1];
    if (top) top.phase = "comma";
  }

  private atTarget(): boolean {
    if (this.stack.length !== this.fieldPath.length) return false;
    return this.stack.every((frame, index) => frame.isObject && frame.key === this.fieldPath[index]);
  }

  private beginString(role: StringRole): void {
    this.inString = role;
    this.escape = "none";
    this.keyBuffer = "";
    this.keyOverflow = false;
    this.pendingHigh = null;
    this.stack[this.stack.length - 1].empty = false;
  }

  private consumeStringChar(char: string): void {
    if (this.escape === "slash") {
      if (char === "u") {
        this.escape = "";
        return;
      }
      const mapped = simpleEscapes[char];
      this.escape = "none";
      if (mapped === undefined) return this.fail();
      this.append(mapped.charCodeAt(0));
      return;
    }
    if (this.escape !== "none") {
      if (!/[0-9a-fA-F]/.test(char)) return this.fail();
      this.escape += char;
      if (this.escape.length === 4) {
        const code = Number.parseInt(this.escape, 16);
        this.escape = "none";
        this.append(code);
      }
      return;
    }
    if (char === "\\") {
      this.escape = "slash";
      return;
    }
    const unit = char.charCodeAt(0);
    if (char === '"') {
      this.endString();
      return;
    }
    if (unit < 0x20) return this.fail();
    this.append(unit);
  }

  private append(unit: number): void {
    const role = this.inString;
    if (role === "key") {
      if (this.keyBuffer.length >= MAX_KEY_LENGTH) this.keyOverflow = true;
      else this.keyBuffer += String.fromCharCode(unit);
      return;
    }
    if (role !== "target") return;
    if (this.pendingHigh !== null) {
      if (!isLow(unit)) return this.fail();
      this.emit(String.fromCharCode(this.pendingHigh, unit));
      this.pendingHigh = null;
    } else if (isHigh(unit)) {
      this.pendingHigh = unit;
    } else if (isLow(unit)) {
      this.fail();
    } else {
      this.emit(String.fromCharCode(unit));
    }
  }

  private emit(text: string): void {
    if (this.emitted + text.length > MAX_ANSWER_STREAM_CHARS) {
      this.halted = true;
      return;
    }
    this.emitted += text.length;
    this.out.push(text);
  }

  private endString(): void {
    const role = this.inString;
    this.inString = null;
    if (this.escape !== "none" || (role === "target" && this.pendingHigh !== null)) return this.fail();
    const top = this.stack[this.stack.length - 1];
    if (role === "key") {
      top.key = this.keyOverflow ? null : this.keyBuffer;
      top.phase = "colon";
    } else {
      top.phase = "comma";
      if (role === "target") this.halted = true;
    }
  }
}

/** Splits decoded answer text into frame-sized pieces without separating a surrogate pair. */
export function splitAnswerText(text: string): string[] {
  const pieces: string[] = [];
  for (let start = 0; start < text.length;) {
    let end = Math.min(start + MAX_ANSWER_DELTA_CHARS, text.length);
    if (end < text.length && isHigh(text.charCodeAt(end - 1))) end -= 1;
    pieces.push(text.slice(start, end));
    start = end;
  }
  return pieces;
}

/**
 * Per-run emitter. Only the first model round may stream; the round is abandoned for good on any anomaly, on
 * a tool call, or when it ends, so a later or repeated round can never add text under the same message ID.
 */
export class AnswerStream {
  private decoder: AnswerFieldDecoder | null;

  constructor(
    private readonly request: AnswerStreamRequest,
    private readonly emit: (messageId: string, text: string) => Promise<void>,
  ) {
    this.decoder = new AnswerFieldDecoder(request.fieldPath);
  }

  /** Called for each model text delta of the current round. */
  async text(delta: string): Promise<void> {
    const decoder = this.decoder;
    if (!decoder) return;
    const decoded = decoder.push(delta);
    if (decoder.stopped) this.decoder = null;
    for (const piece of splitAnswerText(decoded)) await this.emit(this.request.messageId, piece);
  }

  /** Called when the round ends, a tool call is seen, or a tool is about to run: nothing after this is streamed. */
  close(): void {
    this.decoder = null;
  }
}

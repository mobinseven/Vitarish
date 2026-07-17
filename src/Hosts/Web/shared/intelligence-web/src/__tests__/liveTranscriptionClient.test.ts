import { afterEach, describe, expect, it, vi } from "vitest";
import {
  buildLiveTranscriptionSocketUrl,
  LiveTranscriptionClient,
  selectSupportedLiveTranscriptionMimeType,
} from "../api/intelligence/liveTranscriptionClient";
import type {
  LiveTranscriptionBrowserDependencies,
  LiveDictationFailureCode,
  LiveDictationLifecycle,
} from "../api/intelligence/liveTranscriptionClient";
import {
  appendConfirmedTranscript,
  selectDictationLanguage,
} from "../hooks/useLiveDictation";

afterEach(() => {
  vi.unstubAllGlobals();
});

describe("live transcription browser adapter", () => {
  it("derives the WebSocket URL from the generated capability route", () => {
    expect(buildLiveTranscriptionSocketUrl("https://api.example.test", "https://app.example.test"))
      .toBe("wss://api.example.test/api/modules/intelligence/transcription/live");
    expect(buildLiveTranscriptionSocketUrl("", "http://localhost:3000"))
      .toBe("ws://localhost:3000/api/modules/intelligence/transcription/live");
  });

  it("selects only a server-advertised codec supported by MediaRecorder", () => {
    vi.stubGlobal("MediaRecorder", class {
      static isTypeSupported(mimeType: string) {
        return mimeType === "audio/webm";
      }
    });

    expect(selectSupportedLiveTranscriptionMimeType([
      "audio/webm;codecs=opus",
      "audio/webm",
    ])).toBe("audio/webm");
    expect(selectSupportedLiveTranscriptionMimeType(["audio/ogg"])).toBeNull();
  });

  it("preserves the latest draft while appending confirmed segments", () => {
    expect(appendConfirmedTranscript("Existing edit", " confirmed phrase "))
      .toBe("Existing edit confirmed phrase");
    expect(appendConfirmedTranscript("Existing edit\n", "confirmed phrase"))
      .toBe("Existing edit\nconfirmed phrase");
  });

  it("tracks locale changes while preserving a supported manual language selection", () => {
    const languages = ["fa-IR", "en-US"];

    expect(selectDictationLanguage(languages, "en-US", "fa-IR", "en-US"))
      .toBe("fa-IR");
    expect(selectDictationLanguage(languages, "en-US", "fa-IR", "fa-IR"))
      .toBe("en-US");
    expect(selectDictationLanguage(languages, "de-DE", "fa-IR", "fa-IR"))
      .toBe("fa-IR");
  });

  it("sends ordered bounded audio, accepts ordered final segments, and cleans up after stop", async () => {
    const socket = new FakeWebSocket();
    const recorder = new FakeMediaRecorder();
    const track = { stop: vi.fn() };
    const states: LiveDictationLifecycle[] = [];
    const segments: string[] = [];
    const failures: LiveDictationFailureCode[] = [];
    const client = new LiveTranscriptionClient(
      configuration(),
      {
        onFailure: (failure) => failures.push(failure),
        onSegment: (segment) => segments.push(segment),
        onStateChange: (state) => states.push(state),
      },
      dependencies(socket, recorder, track),
    );

    const started = client.start();
    await Promise.resolve();
    socket.open();
    expect(JSON.parse(String(socket.sent[0]))).toEqual({
      language: "fa-IR",
      mimeType: "audio/webm;codecs=opus",
      type: "start",
    });
    socket.receive({ sessionId: "session-1", type: "started" });
    await started;

    recorder.emit(audioBlob([1, 2, 3]));
    recorder.emit(audioBlob([4]));
    const stopping = client.stop();
    await stopping;

    const audioFrames = socket.sent.slice(1, -1) as ArrayBuffer[];
    expect(await Promise.all(audioFrames.map(async (frame) => [...new Uint8Array(frame)])))
      .toEqual([[1, 2], [3], [4]]);
    expect(JSON.parse(String(socket.sent.at(-1)))).toEqual({ type: "stop" });

    socket.receive({ sequence: 0, sessionId: "session-1", text: "first", type: "segment" });
    socket.receive({ sequence: 1, sessionId: "session-1", text: "second", type: "segment" });
    socket.receive({ sessionId: "session-1", type: "completed" });

    expect(segments).toEqual(["first", "second"]);
    expect(failures).toEqual([]);
    expect(states).toEqual(["connecting", "recording", "completing", "idle"]);
    expect(track.stop).toHaveBeenCalledOnce();
    expect(socket.closeCalls).toBe(1);
  });

  it("retains confirmed text and fails on duplicate or out-of-order segments", async () => {
    const socket = new FakeWebSocket();
    const recorder = new FakeMediaRecorder();
    const segments: string[] = [];
    const failures: LiveDictationFailureCode[] = [];
    const track = { stop: vi.fn() };
    const client = new LiveTranscriptionClient(
      configuration(),
      {
        onFailure: (failure) => failures.push(failure),
        onSegment: (segment) => segments.push(segment),
        onStateChange: () => undefined,
      },
      dependencies(socket, recorder, track),
    );

    const started = client.start();
    await Promise.resolve();
    socket.open();
    socket.receive({ sessionId: "session-2", type: "started" });
    await started;
    socket.receive({ sequence: 0, sessionId: "session-2", text: "confirmed", type: "segment" });
    socket.receive({ sequence: 0, sessionId: "session-2", text: "duplicate", type: "segment" });

    expect(segments).toEqual(["confirmed"]);
    expect(failures).toEqual(["ProtocolViolation"]);
    expect(track.stop).toHaveBeenCalledOnce();
  });

  it("maps microphone denial and makes repeated cleanup idempotent", async () => {
    const failure = Object.assign(new Error("denied"), { name: "NotAllowedError" });
    const failures: LiveDictationFailureCode[] = [];
    const browser: LiveTranscriptionBrowserDependencies = {
      clearTimer: vi.fn(),
      createMediaRecorder: () => new FakeMediaRecorder(),
      createWebSocket: () => new FakeWebSocket(),
      getUserMedia: vi.fn().mockRejectedValue(failure),
      setTimer: vi.fn(() => 1),
    };
    const client = new LiveTranscriptionClient(
      configuration(),
      {
        onFailure: (code) => failures.push(code),
        onSegment: () => undefined,
        onStateChange: () => undefined,
      },
      browser,
    );

    await expect(client.start()).rejects.toThrow();
    client.dispose();
    client.dispose();
    expect(failures).toEqual(["PermissionDenied"]);
  });
});

function configuration() {
  return {
    language: "fa-IR",
    limits: { idleTimeoutSeconds: 20, maxFrameBytes: 2, maxTotalBytes: 20 },
    mimeType: "audio/webm;codecs=opus",
    socketUrl: "wss://api.example.test/api/modules/intelligence/transcription/live",
  };
}

function audioBlob(bytes: number[]): Blob {
  const audio = Uint8Array.from(bytes);
  return {
    arrayBuffer: async () => audio.buffer,
    size: audio.byteLength,
  } as Blob;
}

function dependencies(
  socket: FakeWebSocket,
  recorder: FakeMediaRecorder,
  track: { stop: () => void },
): LiveTranscriptionBrowserDependencies {
  return {
    clearTimer: vi.fn(),
    createMediaRecorder: () => recorder,
    createWebSocket: () => socket,
    getUserMedia: async () => ({ getTracks: () => [track] }),
    setTimer: vi.fn(() => 1),
  };
}

class FakeMediaRecorder {
  ondataavailable: ((event: BlobEvent) => void) | null = null;
  onerror: ((event: ErrorEvent) => void) | null = null;
  onstop: ((event: Event) => void) | null = null;
  state: RecordingState = "inactive";

  start() {
    this.state = "recording";
  }

  stop() {
    this.state = "inactive";
    this.onstop?.(new Event("stop"));
  }

  emit(data: Blob) {
    this.ondataavailable?.({ data } as BlobEvent);
  }
}

class FakeWebSocket {
  closeCalls = 0;
  onclose: ((event: CloseEvent) => void) | null = null;
  onerror: ((event: Event) => void) | null = null;
  onmessage: ((event: MessageEvent<unknown>) => void) | null = null;
  onopen: ((event: Event) => void) | null = null;
  readyState = 0;
  sent: Array<string | ArrayBuffer | ArrayBufferView | Blob> = [];

  open() {
    this.readyState = 1;
    this.onopen?.(new Event("open"));
  }

  receive(message: unknown) {
    this.onmessage?.(new MessageEvent("message", { data: JSON.stringify(message) }));
  }

  send(data: string | ArrayBuffer | ArrayBufferView | Blob) {
    this.sent.push(data);
  }

  close() {
    this.closeCalls += 1;
    this.readyState = 3;
  }
}

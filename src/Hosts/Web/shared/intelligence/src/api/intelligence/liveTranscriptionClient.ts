import { getIntelligenceGetLiveTranscriptionCapabilitiesQueryKey } from "../generated/intelligence/intelligence";

export const liveTranscriptionSubprotocol = "vitarish.live-transcription.v1";

export type LiveDictationLifecycle =
  | "idle"
  | "connecting"
  | "recording"
  | "completing"
  | "failed";

export type LiveDictationFailureCode =
  | "CapabilityUnavailable"
  | "InsecureContext"
  | "BrowserUnsupported"
  | "MicrophoneUnavailable"
  | "PermissionDenied"
  | "CodecUnavailable"
  | "NetworkUnavailable"
  | "InvalidStart"
  | "ProtocolViolation"
  | "UnsupportedLanguage"
  | "UnsupportedMediaType"
  | "FrameTooLarge"
  | "PayloadTooLarge"
  | "DurationLimitExceeded"
  | "IdleTimeout"
  | "ProviderUnavailable"
  | "ProviderTimeout"
  | "InvalidAudio"
  | "Cancellation"
  | "UnexpectedFailure";

export interface LiveTranscriptionClientLimits {
  idleTimeoutSeconds: number;
  maxFrameBytes: number;
  maxTotalBytes: number;
}

export interface LiveTranscriptionClientConfiguration {
  language: string;
  limits: LiveTranscriptionClientLimits;
  mimeType: string;
  socketUrl: string;
}

export interface LiveTranscriptionClientCallbacks {
  onFailure: (code: LiveDictationFailureCode) => void;
  onSegment: (text: string) => void;
  onStateChange: (state: LiveDictationLifecycle) => void;
}

interface MediaStreamTrackLike {
  stop: () => void;
}

interface MediaStreamLike {
  getTracks: () => MediaStreamTrackLike[];
}

interface MediaRecorderLike {
  ondataavailable: ((event: BlobEvent) => void) | null;
  onerror: ((event: ErrorEvent) => void) | null;
  onstop: ((event: Event) => void) | null;
  state: RecordingState;
  start: (timeslice?: number) => void;
  stop: () => void;
}

interface WebSocketLike {
  onclose: ((event: CloseEvent) => void) | null;
  onerror: ((event: Event) => void) | null;
  onmessage: ((event: MessageEvent<unknown>) => void) | null;
  onopen: ((event: Event) => void) | null;
  readyState: number;
  close: (code?: number, reason?: string) => void;
  send: (data: string | ArrayBuffer | ArrayBufferView | Blob) => void;
}

export interface LiveTranscriptionBrowserDependencies {
  clearTimer: (timer: number) => void;
  createMediaRecorder: (stream: MediaStreamLike, mimeType: string) => MediaRecorderLike;
  createWebSocket: (url: string, protocol: string) => WebSocketLike;
  getUserMedia: () => Promise<MediaStreamLike>;
  setTimer: (callback: () => void, milliseconds: number) => number;
}

interface RealtimeServerMessage {
  error?: { code?: unknown };
  sequence?: unknown;
  sessionId?: unknown;
  text?: unknown;
  type?: unknown;
}

const webSocketConnecting = 0;
const webSocketOpen = 1;
const knownServerFailureCodes = new Set<LiveDictationFailureCode>([
  "InvalidStart",
  "ProtocolViolation",
  "UnsupportedLanguage",
  "UnsupportedMediaType",
  "FrameTooLarge",
  "PayloadTooLarge",
  "DurationLimitExceeded",
  "IdleTimeout",
  "ProviderUnavailable",
  "ProviderTimeout",
  "InvalidAudio",
  "Cancellation",
  "UnexpectedFailure",
]);

export function getDefaultLiveTranscriptionBrowserDependencies(): LiveTranscriptionBrowserDependencies {
  return {
    clearTimer: (timer) => window.clearTimeout(timer),
    createMediaRecorder: (stream, mimeType) =>
      new MediaRecorder(stream as MediaStream, { audioBitsPerSecond: 32_000, mimeType }),
    createWebSocket: (url, protocol) => new WebSocket(url, protocol),
    getUserMedia: () =>
      navigator.mediaDevices.getUserMedia({
        audio: {
          autoGainControl: true,
          echoCancellation: true,
          noiseSuppression: true,
        },
        video: false,
      }),
    setTimer: (callback, milliseconds) => window.setTimeout(callback, milliseconds),
  };
}

export function getDictationBrowserFailureCode(): LiveDictationFailureCode | null {
  if (typeof window === "undefined" || typeof navigator === "undefined") {
    return "BrowserUnsupported";
  }

  if (!window.isSecureContext) return "InsecureContext";
  if (!navigator.mediaDevices?.getUserMedia || typeof MediaRecorder === "undefined") {
    return "BrowserUnsupported";
  }

  if (typeof WebSocket === "undefined") return "BrowserUnsupported";
  return null;
}

export function selectSupportedLiveTranscriptionMimeType(
  mimeTypes: readonly string[],
): string | null {
  if (typeof MediaRecorder === "undefined" || typeof MediaRecorder.isTypeSupported !== "function") {
    return null;
  }

  return mimeTypes.find((mimeType) => MediaRecorder.isTypeSupported(mimeType)) ?? null;
}

export function buildLiveTranscriptionSocketUrl(
  apiBaseUrl = process.env.NEXT_PUBLIC_API_BASE_URL ?? "",
  locationOrigin = typeof window === "undefined" ? "" : window.location.origin,
): string {
  if (!locationOrigin && !apiBaseUrl) {
    throw new Error("A browser origin is required for live transcription.");
  }

  const [capabilityPath] = getIntelligenceGetLiveTranscriptionCapabilitiesQueryKey();
  if (!capabilityPath.endsWith("/capabilities")) {
    throw new Error("The generated live transcription capability route is invalid.");
  }

  const socketPath = capabilityPath.slice(0, -"/capabilities".length);
  const base = (apiBaseUrl.trim() || locationOrigin).replace(/\/$/u, "");
  const url = new URL(`${base}${socketPath}`, locationOrigin || undefined);
  if (url.protocol === "https:") url.protocol = "wss:";
  else if (url.protocol === "http:") url.protocol = "ws:";
  else throw new Error("Live transcription requires an HTTP or HTTPS API origin.");
  return url.toString();
}

export class LiveTranscriptionClient {
  private readonly started: Promise<void>;
  private resolveStarted: (() => void) | undefined;
  private rejectStarted: ((error: Error) => void) | undefined;
  private recorder: MediaRecorderLike | undefined;
  private socket: WebSocketLike | undefined;
  private stream: MediaStreamLike | undefined;
  private timer: number | undefined;
  private stopPromise: Promise<void> | undefined;
  private resolveRecorderStop: (() => void) | undefined;
  private sendChain: Promise<void> = Promise.resolve();
  private lifecycle: LiveDictationLifecycle = "idle";
  private sessionId: string | undefined;
  private nextSequence = 0;
  private sentBytes = 0;
  private startedOnce = false;
  private terminal = false;

  constructor(
    private readonly configuration: LiveTranscriptionClientConfiguration,
    private readonly callbacks: LiveTranscriptionClientCallbacks,
    private readonly browser: LiveTranscriptionBrowserDependencies =
      getDefaultLiveTranscriptionBrowserDependencies(),
  ) {
    this.started = new Promise<void>((resolve, reject) => {
      this.resolveStarted = resolve;
      this.rejectStarted = reject;
    });
  }

  async start(): Promise<void> {
    if (this.startedOnce) throw new Error("A dictation client can only be started once.");
    this.startedOnce = true;
    this.setLifecycle("connecting");

    try {
      this.stream = await this.browser.getUserMedia();
      if (this.terminal) {
        this.stopTracks();
        return await this.started;
      }

      this.recorder = this.browser.createMediaRecorder(
        this.stream,
        this.configuration.mimeType,
      );
      this.recorder.ondataavailable = (event) => this.enqueueAudio(event.data);
      this.recorder.onerror = () => this.fail("InvalidAudio");

      this.socket = this.browser.createWebSocket(
        this.configuration.socketUrl,
        liveTranscriptionSubprotocol,
      );
      this.socket.onopen = () => this.sendStart();
      this.socket.onmessage = (event) => this.handleMessage(event.data);
      this.socket.onerror = () => this.fail("NetworkUnavailable");
      this.socket.onclose = () => {
        if (!this.terminal) this.fail("NetworkUnavailable");
      };
      this.armTimer(
        "NetworkUnavailable",
        Math.max(5, this.configuration.limits.idleTimeoutSeconds + 2),
      );
    } catch (error: unknown) {
      this.fail(toBrowserFailureCode(error));
    }

    return await this.started;
  }

  stop(): Promise<void> {
    if (this.stopPromise) return this.stopPromise;
    if (this.lifecycle !== "recording" || this.terminal) return Promise.resolve();

    this.setLifecycle("completing");
    this.armTimer(
      "NetworkUnavailable",
      Math.max(5, this.configuration.limits.idleTimeoutSeconds + 2),
    );
    this.stopPromise = this.stopAndFlushAsync();
    return this.stopPromise;
  }

  dispose(): void {
    if (this.terminal) {
      this.cleanup();
      return;
    }

    this.terminal = true;
    this.rejectStarted?.(new Error("Live transcription was canceled."));
    this.resolveRecorderStop?.();
    this.cleanup();
  }

  private sendStart(): void {
    if (this.terminal || this.lifecycle !== "connecting" || !this.socket) return;
    try {
      this.socket.send(JSON.stringify({
        language: this.configuration.language,
        mimeType: this.configuration.mimeType,
        type: "start",
      }));
    } catch {
      this.fail("NetworkUnavailable");
    }
  }

  private handleMessage(data: unknown): void {
    if (this.terminal || typeof data !== "string") {
      if (!this.terminal) this.fail("ProtocolViolation");
      return;
    }

    let message: RealtimeServerMessage;
    try {
      const parsed: unknown = JSON.parse(data);
      if (!parsed || typeof parsed !== "object") throw new Error("Invalid message.");
      message = parsed as RealtimeServerMessage;
    } catch {
      this.fail("ProtocolViolation");
      return;
    }

    if (typeof message.type !== "string" || typeof message.sessionId !== "string") {
      this.fail("ProtocolViolation");
      return;
    }

    if (message.type === "started") {
      this.handleStarted(message.sessionId);
      return;
    }

    if (this.sessionId && message.sessionId !== this.sessionId) {
      this.fail("ProtocolViolation");
      return;
    }

    if (message.type === "segment") {
      this.handleSegment(message);
    } else if (message.type === "completed") {
      this.handleCompleted();
    } else if (message.type === "failed" || message.type === "canceled") {
      this.fail(readServerFailureCode(message));
    } else {
      this.fail("ProtocolViolation");
    }
  }

  private handleStarted(sessionId: string): void {
    if (this.lifecycle !== "connecting" || this.sessionId || !sessionId || !this.recorder) {
      this.fail("ProtocolViolation");
      return;
    }

    this.sessionId = sessionId;
    this.clearTimer();
    try {
      this.recorder.start(1_000);
    } catch {
      this.fail("CodecUnavailable");
      return;
    }

    this.setLifecycle("recording");
    this.resolveStarted?.();
  }

  private handleSegment(message: RealtimeServerMessage): void {
    if (
      (this.lifecycle !== "recording" && this.lifecycle !== "completing")
      || typeof message.sequence !== "number"
      || !Number.isSafeInteger(message.sequence)
      || message.sequence !== this.nextSequence
      || typeof message.text !== "string"
      || !message.text.trim()
    ) {
      this.fail("ProtocolViolation");
      return;
    }

    this.nextSequence += 1;
    try {
      this.callbacks.onSegment(message.text.trim());
    } catch {
      this.fail("UnexpectedFailure");
    }
  }

  private handleCompleted(): void {
    if (this.lifecycle !== "completing") {
      this.fail("ProtocolViolation");
      return;
    }

    this.terminal = true;
    this.clearTimer();
    this.cleanup();
    this.setLifecycle("idle");
  }

  private enqueueAudio(blob: Blob): void {
    if (this.terminal || blob.size === 0) return;

    this.sendChain = this.sendChain
      .then(async () => {
        const audio = await blob.arrayBuffer();
        if (this.terminal || audio.byteLength === 0) return;
        if (this.sentBytes + audio.byteLength > this.configuration.limits.maxTotalBytes) {
          this.fail("PayloadTooLarge");
          return;
        }

        this.sentBytes += audio.byteLength;
        for (
          let offset = 0;
          offset < audio.byteLength;
          offset += this.configuration.limits.maxFrameBytes
        ) {
          if (this.terminal) return;
          if (!this.socket || this.socket.readyState !== webSocketOpen) {
            throw new Error("The dictation socket is unavailable.");
          }

          this.socket.send(audio.slice(
            offset,
            Math.min(offset + this.configuration.limits.maxFrameBytes, audio.byteLength),
          ));
        }
      })
      .catch(() => this.fail("NetworkUnavailable"));
  }

  private async stopAndFlushAsync(): Promise<void> {
    const recorder = this.recorder;
    if (recorder && recorder.state !== "inactive") {
      await new Promise<void>((resolve) => {
        this.resolveRecorderStop = resolve;
        recorder.onstop = () => {
          this.resolveRecorderStop = undefined;
          resolve();
        };
        try {
          recorder.stop();
        } catch {
          this.resolveRecorderStop = undefined;
          resolve();
          this.fail("InvalidAudio");
        }
      });
    }

    await this.sendChain;
    if (this.terminal) return;
    if (!this.socket || this.socket.readyState !== webSocketOpen) {
      this.fail("NetworkUnavailable");
      return;
    }

    try {
      this.socket.send(JSON.stringify({ type: "stop" }));
    } catch {
      this.fail("NetworkUnavailable");
    }
  }

  private fail(code: LiveDictationFailureCode): void {
    if (this.terminal) return;
    this.terminal = true;
    this.clearTimer();
    this.rejectStarted?.(new Error(`Live transcription failed: ${code}`));
    this.resolveRecorderStop?.();
    this.callbacks.onFailure(code);
    this.cleanup();
    this.setLifecycle("failed");
  }

  private setLifecycle(lifecycle: LiveDictationLifecycle): void {
    this.lifecycle = lifecycle;
    this.callbacks.onStateChange(lifecycle);
  }

  private armTimer(code: LiveDictationFailureCode, seconds: number): void {
    this.clearTimer();
    this.timer = this.browser.setTimer(() => this.fail(code), seconds * 1_000);
  }

  private clearTimer(): void {
    if (this.timer === undefined) return;
    this.browser.clearTimer(this.timer);
    this.timer = undefined;
  }

  private stopTracks(): void {
    for (const track of this.stream?.getTracks() ?? []) track.stop();
    this.stream = undefined;
  }

  private cleanup(): void {
    this.clearTimer();
    if (this.recorder) {
      this.recorder.ondataavailable = null;
      this.recorder.onerror = null;
      this.recorder.onstop = null;
      if (this.recorder.state !== "inactive") {
        try {
          this.recorder.stop();
        } catch {
          // The recorder may already be stopping; tracks are still released below.
        }
      }
    }

    this.resolveRecorderStop?.();
    this.resolveRecorderStop = undefined;
    this.stopTracks();
    if (this.socket) {
      this.socket.onopen = null;
      this.socket.onmessage = null;
      this.socket.onerror = null;
      this.socket.onclose = null;
      if (
        this.socket.readyState === webSocketConnecting
        || this.socket.readyState === webSocketOpen
      ) {
        try {
          this.socket.close(1_000, "Live transcription ended.");
        } catch {
          // Closing is best-effort after the terminal client state is already stable.
        }
      }
    }
  }
}

function readServerFailureCode(message: RealtimeServerMessage): LiveDictationFailureCode {
  const code = message.error?.code;
  return typeof code === "string"
    && knownServerFailureCodes.has(code as LiveDictationFailureCode)
    ? code as LiveDictationFailureCode
    : "UnexpectedFailure";
}

function toBrowserFailureCode(error: unknown): LiveDictationFailureCode {
  const name = typeof error === "object" && error !== null && "name" in error
    ? String((error as { name: unknown }).name)
    : "";
  if (name === "NotAllowedError" || name === "SecurityError") return "PermissionDenied";
  if (
    name === "NotFoundError"
    || name === "NotReadableError"
    || name === "OverconstrainedError"
    || name === "AbortError"
  ) {
    return "MicrophoneUnavailable";
  }

  if (name === "NotSupportedError") return "CodecUnavailable";
  return "BrowserUnsupported";
}

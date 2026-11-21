import { Agent, OpenAIProvider, Runner, tool, type Model, type ModelProvider, type StreamEvent } from "@openai/agents";
import { NextRequest } from "next/server";
import { AGENT_CREDENTIAL_HEADER } from "../credential-header";
import { AnswerFieldDecoder, AnswerStream, isValidAnswerStreamRequest, type AnswerStreamRequest } from "./answer-stream";
import { AgentRunFailure, agentRunFailureWire, type AgentRunFailureCode } from "./failure-codes";
import { parseHistory } from "./history";
import { surfaceProblem } from "./surface";
import { adaptToolParameters } from "./tool-parameters";

interface AgentDefinition {
  name?: string | null;
  model: string;
  instructions: string;
  reasoningLevel?: "none" | "minimal" | "low" | "medium" | "high" | "xhigh" | "max";
  result?: { name: string; jsonSchema: string; strict: boolean } | null;
}

interface AgentToolDefinition {
  name: string;
  description: string;
  parametersJsonSchema: string;
  /** For a tool whose argument is a presented surface: the catalog it must satisfy (ADR-0022 decision 8). */
  surfaceCatalog?: string | null;
}

const agentSubjectKinds = ["patient", "practice", "event", "landing-page", "platform-home"] as const;
type AgentSubjectKind = (typeof agentSubjectKinds)[number];

interface AgentRuntimeExecutionEnvelope {
  version: number;
  executionId: string;
  agentKey: string;
  tenantId: string;
  practiceId: string;
  subjectKind: AgentSubjectKind;
  subjectId: string;
  authenticatedUserId: string;
  conversationId: string;
  toolCallId: string;
  toolName: string;
  credentialVersion: number;
  expiresAt: string;
  correlationId: string;
  role: string;
  locale: string;
  timeZone: string;
  calendar: string;
  currentUtc: string;
  currentLocal: string;
  permissions: string[];
  patientNationalId?: string | null;
  callbackSignature?: string | null;
}

interface AgentRuntimeRequest {
  definition: AgentDefinition;
  input: { userInput: string; history?: string | null };
  tools: AgentToolDefinition[];
  executionEnvelope: AgentRuntimeExecutionEnvelope;
  limits: {
    maxIterations: number;
    maxToolCalls: number;
    maxTokens: number;
    effectiveTimeout: string;
  };
  callbackUrl: string;
  callbackCredential: string;
  /**
   * Declares in advance that the answer string at `answerStream.fieldPath` of the structured result may be
   * streamed as provisional text. Honored only by a streamed run of a tool-free request with a result schema;
   * absent or ineligible means progress-only.
   */
  answerStream?: AnswerStreamRequest | null;
  /**
   * 1-based ordinal of this runtime attempt of the turn, owned by Main (a history retry is a later attempt);
   * absent means the first. Reported in the per-call log record only: model-round ordinals count within the attempt.
   */
  attempt?: number | null;
}

const reasoningLevels = new Set(["none", "minimal", "low", "medium", "high", "xhigh", "max"]);
const MAX_ATTEMPT_ORDINAL = 1000;
const envelopeVersion = 1;
const credentialVersion = 1;

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null && !Array.isArray(value);
}

function isText(value: unknown): value is string {
  return typeof value === "string" && value.trim().length > 0;
}

function isGuid(value: string): boolean {
  return /^[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}$/i.test(value);
}

function isValidExecutionEnvelope(value: unknown): value is AgentRuntimeExecutionEnvelope {
  if (!isRecord(value) || value.version !== envelopeVersion || value.credentialVersion !== credentialVersion) return false;
  const envelope = value as Partial<AgentRuntimeExecutionEnvelope>;
  const required = [
    envelope.executionId,
    envelope.agentKey,
    envelope.tenantId,
    envelope.practiceId,
    envelope.subjectKind,
    envelope.subjectId,
    envelope.authenticatedUserId,
    envelope.conversationId,
    envelope.correlationId,
    envelope.role,
    envelope.locale,
    envelope.timeZone,
    envelope.calendar,
    envelope.currentUtc,
    envelope.currentLocal,
    envelope.expiresAt,
  ];
  if (!required.every(isText) || !Array.isArray(envelope.permissions) || !envelope.permissions.every(isText)) return false;
  const typedEnvelope = envelope as AgentRuntimeExecutionEnvelope;
  const expiresAt = Date.parse(typedEnvelope.expiresAt);
  if (!Number.isFinite(expiresAt) || expiresAt <= Date.now() || expiresAt > Date.now() + 15 * 60 * 1000) return false;
  if (!Number.isFinite(Date.parse(typedEnvelope.currentUtc)) || !Number.isFinite(Date.parse(typedEnvelope.currentLocal))) return false;
  if (!agentSubjectKinds.includes(typedEnvelope.subjectKind)) return false;
  if (typedEnvelope.subjectKind === "patient" && !isGuid(typedEnvelope.subjectId)) return false;
  if (typedEnvelope.subjectKind === "event" && !isGuid(typedEnvelope.subjectId)) return false;
  if (typedEnvelope.subjectKind === "practice" && (typedEnvelope.subjectId !== typedEnvelope.practiceId || typedEnvelope.tenantId !== typedEnvelope.practiceId)) return false;
  return true;
}

interface ToolExecution {
  toolName: string;
  toolCallId: string;
  output: string;
  isError: boolean;
  argumentsJson: string;
}

class AgentRuntimeLimitError extends Error {
  constructor(readonly code: typeof AgentRunFailure.ToolCallLimitExceeded | typeof AgentRunFailure.TokenLimitExceeded | typeof AgentRunFailure.Timeout) {
    super(code);
  }
}

/** Single owner of cumulative token accounting for the buffered and streamed model paths. */
function assertTokenBudgetRemains(maxTokens: number, usedTokens: { value: number }): void {
  if (usedTokens.value >= maxTokens) throw new AgentRuntimeLimitError(AgentRunFailure.TokenLimitExceeded);
}

/**
 * The tokens one completed provider call spent. The Responses adapter normalizes a provider response without
 * usage to zeros before this runtime can see it, so an unreported total is indistinguishable from a reported
 * zero there; a completed call always consumes at least its prompt, so a zero total is unreported. Input plus
 * output stands in when only the total is missing. Returns null when nothing credible was reported.
 */
function reportedTokenSpend(usage: { inputTokens?: number; outputTokens?: number; totalTokens?: number } | undefined): number | null {
  const credible = (value: unknown): value is number => typeof value === "number" && Number.isFinite(value) && value > 0;
  if (credible(usage?.totalTokens)) return usage.totalTokens;
  const parts = (usage?.inputTokens ?? 0) + (usage?.outputTokens ?? 0);
  return credible(parts) ? parts : null;
}

/** Adds reported usage to the cumulative total; usage the provider did not report is treated as budget exhaustion. */
function recordTokenUsage(maxTokens: number, usedTokens: { value: number }, usage: { inputTokens?: number; outputTokens?: number; totalTokens?: number } | undefined): void {
  const total = reportedTokenSpend(usage);
  if (total === null) {
    usedTokens.value = maxTokens;
    throw new AgentRuntimeLimitError(AgentRunFailure.TokenLimitExceeded);
  }
  usedTokens.value += total;
  if (usedTokens.value > maxTokens) throw new AgentRuntimeLimitError(AgentRunFailure.TokenLimitExceeded);
}

/** The provider request every model call of the run uses: its output allowance never exceeds the remaining budget. */
function clampToRemainingBudget<TRequest extends { modelSettings?: { maxTokens?: number | null } }>(
  request: TRequest,
  maxTokens: number,
  usedTokens: { value: number },
): TRequest {
  const remaining = maxTokens - usedTokens.value;
  const requested = request.modelSettings?.maxTokens;
  return {
    ...request,
    modelSettings: { ...request.modelSettings, maxTokens: typeof requested === "number" ? Math.min(requested, remaining) : remaining },
  };
}

type ReportedUsage = {
  inputTokens?: number;
  outputTokens?: number;
  inputTokensDetails?: Record<string, number> | Array<Record<string, number>>;
  outputTokensDetails?: Record<string, number> | Array<Record<string, number>>;
};

/** Sums one named counter of a provider usage detail, reported either as one record or as a record per request. */
function usageDetail(details: ReportedUsage["inputTokensDetails"], name: string): number {
  return (Array.isArray(details) ? details : details ? [details] : []).reduce((total, entry) => total + (entry[name] ?? 0), 0);
}

const reasoningTokensDetail = "reasoning_tokens";
const cachedTokensDetail = "cached_tokens";

/**
 * Single owner of the per-model-call record for the buffered and streamed paths. Durations and counts only:
 * prompts, tool arguments and replies are clinical data and are never logged. The turn is the model-call ordinal
 * within the runtime attempt, the same number a streamed run reports as the model round ordinal.
 */
function logModelCall(executionId: string, attempt: number, turn: number, streamed: boolean, startedAt: number, usage: ReportedUsage | undefined): void {
  console.info("Agent model call completed", {
    executionId,
    attempt,
    turn,
    streamed,
    durationMs: Math.round(performance.now() - startedAt),
    inputTokens: usage?.inputTokens,
    outputTokens: usage?.outputTokens,
    reasoningTokens: usageDetail(usage?.outputTokensDetails, reasoningTokensDetail),
    cachedInputTokens: usageDetail(usage?.inputTokensDetails, cachedTokensDetail),
  });
}

/**
 * Receives trusted run-stage notifications from the single owner of model and tool execution. Each call is
 * awaited by the run, so a slow consumer applies backpressure to the run rather than growing a buffer.
 * Ordinals start at 1: a model-round ordinal counts provider requests, a tool ordinal counts tool calls.
 */
export interface AgentRunObserver {
  modelRoundStarted(ordinal: number): Promise<void>;
  toolStarted(ordinal: number): Promise<void>;
  toolCompleted(ordinal: number): Promise<void>;
  /** Provisional text of the declared answer field; bounded pieces, awaited like every other notification. */
  answerDelta(messageId: string, text: string): Promise<void>;
}

function untilAborted(work: Promise<void>, signal: AbortSignal): Promise<void> {
  if (signal.aborted) return Promise.reject(signal.reason);
  return new Promise<void>((resolve, reject) => {
    const onAbort = () => reject(signal.reason);
    signal.addEventListener("abort", onAbort, { once: true });
    work.then(resolve, reject).finally(() => signal.removeEventListener("abort", onAbort));
  });
}

/** A consumer that stalls or disappears can never hold the run past its timeout or cancellation. */
function guardObserver(observer: AgentRunObserver, signal: AbortSignal): AgentRunObserver {
  return {
    modelRoundStarted: (ordinal) => untilAborted(observer.modelRoundStarted(ordinal), signal),
    toolStarted: (ordinal) => untilAborted(observer.toolStarted(ordinal), signal),
    toolCompleted: (ordinal) => untilAborted(observer.toolCompleted(ordinal), signal),
    answerDelta: (messageId, text) => untilAborted(observer.answerDelta(messageId, text), signal),
  };
}

/**
 * Streamed counterpart of the buffered budget check: usage is accounted on provider completion, and a stream
 * that ends or is cancelled without reported usage exhausts the budget so later rounds and tools are blocked.
 * The caller supplies the already clamped request.
 */
async function* budgetedStream(
  model: Model,
  request: Parameters<Model["getStreamedResponse"]>[0],
  maxTokens: number,
  usedTokens: { value: number },
  executionId: string,
  attempt: number,
  turn: number,
  answer?: AnswerStream,
): AsyncGenerator<StreamEvent> {
  const startedAt = performance.now();
  let accounted = false;
  try {
    for await (const event of model.getStreamedResponse(request)) {
      if (event.type === "response_done") {
        accounted = true;
        answer?.completeRound();
        logModelCall(executionId, attempt, turn, true, startedAt, event.response.usage);
        recordTokenUsage(maxTokens, usedTokens, event.response.usage);
      } else if (event.type === "output_text_delta") {
        await answer?.text(event.delta);
      }
      yield event;
    }
    if (!accounted) recordTokenUsage(maxTokens, usedTokens, undefined);
  } finally {
    answer?.close();
    // Cancellation or provider failure before completion leaves the spend unknown: block any later round or tool.
    if (!accounted) usedTokens.value = maxTokens;
  }
}

/** Per-attempt identity and counter of model rounds: the SDK resolves a fresh model for every round, so the count lives here. */
interface ModelRounds {
  attempt: number;
  completed: number;
}

/** The text of the model's final message exactly as the provider produced it, before the SDK parses it. */
function rawMessageText(output: ReadonlyArray<unknown>): string {
  let text = "";
  for (const item of output) {
    const message = item as { type?: string; content?: Array<{ type?: string; text?: string }> };
    if (message?.type !== "message" || !Array.isArray(message.content)) continue;
    for (const part of message.content) if (part?.type === "output_text" && typeof part.text === "string") text += part.text;
  }
  return text;
}

function budgetedModelProvider(
  maxTokens: number,
  usedTokens: { value: number },
  executionId: string,
  rounds: ModelRounds,
  observer?: AgentRunObserver,
  answer?: AnswerStream,
  answerFieldPath?: readonly string[],
): ModelProvider {
  const provider = new OpenAIProvider();
  return {
    getModel: (modelName?: string) => {
      const model = provider.getModel(modelName);
      if (model instanceof Promise) {
        return model.then((resolved) => budgetedModel(resolved, maxTokens, usedTokens, executionId, rounds, observer, answer, answerFieldPath));
      }
      return budgetedModel(model, maxTokens, usedTokens, executionId, rounds, observer, answer, answerFieldPath);
    },
  };
}

function budgetedModel(
  model: Model,
  maxTokens: number,
  usedTokens: { value: number },
  executionId: string,
  rounds: ModelRounds,
  observer?: AgentRunObserver,
  answer?: AnswerStream,
  answerFieldPath?: readonly string[],
): Model {
  return {
    getResponse: async (request) => {
      assertTokenBudgetRemains(maxTokens, usedTokens);
      rounds.completed += 1;
      const startedAt = performance.now();
      const response = await model.getResponse(clampToRemainingBudget(request, maxTokens, usedTokens));
      logModelCall(executionId, rounds.attempt, rounds.completed, false, startedAt, response.usage);
      recordTokenUsage(maxTokens, usedTokens, response.usage);
      if (answerFieldPath) {
        // Streamed runs validate the live decoder; here the original text is checked before the SDK parse erases duplicates.
        const raw = new AnswerFieldDecoder(answerFieldPath);
        raw.push(rawMessageText(response.output));
        if (!raw.wellFormed) throw new Error(AgentRunFailure.ProviderFailure);
      }
      return response;
    },
    getStreamedResponse: (request) =>
      (async function* () {
        // A round the budget already refuses is never announced.
        assertTokenBudgetRemains(maxTokens, usedTokens);
        rounds.completed += 1;
        const turn = rounds.completed;
        await observer?.modelRoundStarted(turn);
        yield* budgetedStream(model, clampToRemainingBudget(request, maxTokens, usedTokens), maxTokens, usedTokens, executionId, rounds.attempt, turn, answer);
      })(),
    getRetryAdvice: model.getRetryAdvice?.bind(model),
  };
}

function timeSpanToMilliseconds(value: string): number {
  const match = /^(\d+):(\d{2}):(\d{2})(?:\.(\d{1,7}))?$/.exec(value);
  if (!match) throw new Error("The agent timeout is invalid.");
  const fraction = Number(`0.${match[4] ?? "0"}`);
  return (((Number(match[1]) * 60 + Number(match[2])) * 60 + Number(match[3])) + fraction) * 1000;
}

function isAuthorized(request: NextRequest): boolean {
  const expected = process.env.AGENT_RUNTIME_SHARED_SECRET;
  return Boolean(expected) && request.headers.get("authorization") === `Bearer ${expected}`;
}

function toolCallId(details: unknown): string {
  const call = (details as { toolCall?: { callId?: string; id?: string } } | undefined)?.toolCall;
  return call?.callId ?? call?.id ?? crypto.randomUUID();
}

function callbackLocation(value: string): string {
  try {
    const url = new URL(value);
    return `${url.protocol}//${url.host}${url.pathname}`;
  } catch {
    return "invalid-callback-url";
  }
}

function canonicalEnvelope(envelope: AgentRuntimeExecutionEnvelope): string {
  return [
    envelope.version.toString(),
    envelope.executionId,
    envelope.agentKey,
    envelope.tenantId,
    envelope.practiceId,
    envelope.subjectKind,
    envelope.subjectId,
    envelope.authenticatedUserId,
    envelope.conversationId,
    envelope.toolCallId,
    envelope.toolName,
    envelope.credentialVersion.toString(),
    new Date(envelope.expiresAt).getTime().toString(),
    envelope.correlationId,
    envelope.role,
    envelope.locale,
    envelope.timeZone,
    envelope.calendar,
    envelope.currentUtc,
    envelope.currentLocal,
    JSON.stringify([...envelope.permissions].sort()),
    envelope.patientNationalId ?? "",
  ].join("\n");
}

async function signCallbackEnvelope(credential: string, envelope: AgentRuntimeExecutionEnvelope): Promise<string> {
  const key = await crypto.subtle.importKey(
    "raw",
    new TextEncoder().encode(credential),
    { name: "HMAC", hash: "SHA-256" },
    false,
    ["sign"],
  );
  const signature = new Uint8Array(await crypto.subtle.sign("HMAC", key, new TextEncoder().encode(canonicalEnvelope(envelope))));
  let binary = "";
  for (const byte of signature) binary += String.fromCharCode(byte);
  return btoa(binary).replaceAll("+", "-").replaceAll("/", "_").replace(/=+$/, "");
}

export interface PreparedAgentRun {
  input: AgentRuntimeRequest;
  timeoutMs: number;
}

/** Response body of a finished run, success or coded failure; the buffered body and the streamed result frame. */
export type AgentRunResponseBody = { isSuccess: boolean } & Record<string, unknown>;

/**
 * Shared admission for the buffered and streamed routes: service credential, request shape, limits and schemas.
 * Returns the HTTP error response to send before any execution starts, or the validated run.
 */
export async function prepareAgentRun(request: NextRequest): Promise<PreparedAgentRun | Response> {
  if (!isAuthorized(request)) return Response.json({ error: "Unauthorized" }, { status: 401 });

  let input: AgentRuntimeRequest;
  try {
    input = (await request.json()) as AgentRuntimeRequest;
  } catch {
    return Response.json({ error: "Invalid JSON request." }, { status: 400 });
  }

  if (!input?.definition?.model || !input.definition.instructions || !input.input?.userInput ||
      !isValidExecutionEnvelope(input.executionEnvelope) ||
      !input.callbackUrl || !input.callbackCredential ||
      !Array.isArray(input.tools) || input.limits?.maxIterations < 1 ||
      input.limits?.maxToolCalls < 0 || input.limits?.maxTokens < 1 ||
      (input.answerStream != null && (!isValidAnswerStreamRequest(input.answerStream) || !input.definition.result)) ||
      (input.attempt != null && (!Number.isInteger(input.attempt) || input.attempt < 1 || input.attempt > MAX_ATTEMPT_ORDINAL))) {
    return Response.json({ error: "The agent runtime request is invalid." }, { status: 400 });
  }

  let timeoutMs: number;
  try {
    timeoutMs = timeSpanToMilliseconds(input.limits.effectiveTimeout);
    if (input.definition.reasoningLevel && !reasoningLevels.has(input.definition.reasoningLevel)) {
      throw new Error("The reasoning level is invalid.");
    }
    for (const definition of input.tools) JSON.parse(definition.parametersJsonSchema);
    if (input.definition.result) {
      const resultSchema = JSON.parse(input.definition.result.jsonSchema);
      if (!resultSchema || typeof resultSchema !== "object" || Array.isArray(resultSchema)) {
        throw new Error("The result schema must be a JSON object schema.");
      }
    }
    if (!Number.isFinite(timeoutMs) || timeoutMs <= 0) throw new Error("The agent timeout is invalid.");
  } catch {
    return Response.json({ error: "The agent runtime request contains invalid limits or tool schemas." }, { status: 400 });
  }

  return { input, timeoutMs };
}

/** Single owner of the coded failure body shared by the buffered response and the streamed result frame. */
export function agentRunFailureBody(code: AgentRunFailureCode, executions: ToolExecution[] = []): AgentRunResponseBody {
  return {
    isSuccess: false,
    output: null,
    structuredOutput: null,
    usage: { inputTokens: 0, outputTokens: 0 },
    iterations: 0,
    toolCalls: executions.length,
    toolExecutions: executions,
    error: agentRunFailureWire[code],
  };
}

/**
 * Runs the prepared agent to completion and returns the response body for either outcome; it never throws.
 * `signal` is the caller's cancellation (client disconnect). With an `observer` the run uses Runner streaming
 * and reports trusted stage notifications; without one it is the buffered run.
 */
export async function executeAgentRun(
  { input, timeoutMs }: PreparedAgentRun,
  signal: AbortSignal,
  observer?: AgentRunObserver,
): Promise<AgentRunResponseBody> {
  const executions: ToolExecution[] = [];
  let toolCallsUsed = 0;
  const usedTokens = { value: 0 };
  let terminalLimit: AgentRuntimeLimitError | undefined;
  const rounds: ModelRounds = { attempt: input.attempt ?? 1, completed: 0 };
  const timeoutController = new AbortController();
  const timeout = setTimeout(() => timeoutController.abort(), timeoutMs);
  const runSignal = AbortSignal.any([signal, timeoutController.signal]);
  const guardedObserver = observer && guardObserver(observer, runSignal);
  // Streamable only when declared by the request, in a streamed run, with no tool the model could call instead.
  const answerStream = guardedObserver && input.answerStream && input.tools.length === 0
    ? new AnswerStream(input.answerStream, guardedObserver.answerDelta)
    : undefined;
  const tools = input.tools.map((definition) => {
    const parameters = adaptToolParameters(JSON.parse(definition.parametersJsonSchema));

    return tool({
      name: definition.name,
      description: definition.description,
      parameters: parameters.parameters as never,
      strict: true,
      timeoutMs: 120_000,
      // The SDK turns a thrown tool error into a tool result and continues. An exhausted limit must end the run instead:
      // it is recorded as terminal and rethrown (the SDK wraps it, so the failure code is read from the record).
      // Every other tool error stays a recoverable result the model can react to, as with the SDK default.
      errorFunction: (_context: unknown, error: unknown) => {
        if (error instanceof AgentRuntimeLimitError) {
          terminalLimit ??= error;
          throw error;
        }
        return `An error occurred while running the tool. Please try again. Error: ${String(error)}`;
      },
      execute: async (argumentsValue: unknown, _context: unknown, details: unknown) => {
        argumentsValue = parameters.decodeArguments(argumentsValue);
        answerStream?.close();
        const callId = toolCallId(details);
        assertTokenBudgetRemains(input.limits.maxTokens, usedTokens);
        if (toolCallsUsed >= input.limits.maxToolCalls) {
          throw new AgentRuntimeLimitError(AgentRunFailure.ToolCallLimitExceeded);
        }
        toolCallsUsed += 1;
        const ordinal = toolCallsUsed;
        await guardedObserver?.toolStarted(ordinal);
        const output = await (async (): Promise<string> => {
          if (definition.surfaceCatalog) {
            const problem = surfaceProblem(definition.surfaceCatalog, argumentsValue);
            if (problem) {
              executions.push({ toolName: definition.name, toolCallId: callId, output: problem, isError: true, argumentsJson: JSON.stringify(argumentsValue) });
              return problem;
            }
          }
          const location = callbackLocation(input.callbackUrl);
          console.info("Calling supplied agent tool callback", {
            executionId: input.executionEnvelope.executionId,
            toolName: definition.name,
            callback: location,
          });
          let response: Response;
          const callbackStartedAt = performance.now();
          try {
            const callbackEnvelope: AgentRuntimeExecutionEnvelope = {
              ...input.executionEnvelope,
              toolCallId: callId,
              toolName: definition.name,
              callbackSignature: null,
            };
            callbackEnvelope.callbackSignature = await signCallbackEnvelope(input.callbackCredential, callbackEnvelope);
            response = await fetch(input.callbackUrl, {
              method: "POST",
              headers: {
                "Content-Type": "application/json",
                [AGENT_CREDENTIAL_HEADER]: input.callbackCredential,
              },
              body: JSON.stringify({
                executionEnvelope: callbackEnvelope,
                argumentsJson: JSON.stringify(argumentsValue),
                surfaceValidated: Boolean(definition.surfaceCatalog),
              }),
              signal: AbortSignal.any([runSignal, AbortSignal.timeout(120_000)]),
            });
          } catch (error) {
            const detail = error instanceof Error ? error : new Error(String(error));
            console.error("Supplied agent tool callback fetch failed", {
              executionId: input.executionEnvelope.executionId,
              toolName: definition.name,
              callback: location,
              errorName: detail.name,
            });
            throw error;
          }

          if (!response.ok) {
            const message = `Agent tool ${definition.name} returned HTTP ${response.status}.`;
            console.error("Supplied agent tool callback returned failure", {
              executionId: input.executionEnvelope.executionId,
              toolName: definition.name,
              callback: location,
              status: response.status,
            });
            executions.push({ toolName: definition.name, toolCallId: callId, output: message, isError: true, argumentsJson: JSON.stringify(argumentsValue) });
            return message;
          }

          const result = (await response.json()) as { callId: string; output: string; isError: boolean };
          executions.push({
            toolName: definition.name,
            toolCallId: result.callId,
            output: result.output,
            isError: result.isError,
            argumentsJson: JSON.stringify(argumentsValue),
          });
          console.info("Supplied agent tool callback succeeded", {
            executionId: input.executionEnvelope.executionId,
            toolName: definition.name,
            status: response.status,
            isToolError: result.isError,
            callbackDurationMs: Math.round(performance.now() - callbackStartedAt),
          });
          return result.output;
        })();
        await guardedObserver?.toolCompleted(ordinal);
        return output;
      },
    });
  });

  console.info("Received supplied agent tools", {
    executionId: input.executionEnvelope.executionId,
    toolCount: tools.length,
    toolNames: input.tools.map((definition) => definition.name).sort(),
  });

  try {
    const priorHistory = parseHistory(input.input.history);
    const outputType = input.definition.result
      ? {
          type: "json_schema" as const,
          name: input.definition.result.name,
          strict: input.definition.result.strict,
          schema: JSON.parse(input.definition.result.jsonSchema) as never,
        }
      : undefined;
    const agent = new Agent({
      name: input.definition.name ?? "Vitarish patient workspace agent",
      model: input.definition.model,
      instructions: input.definition.instructions,
      tools,
      modelSettings: {
        maxTokens: input.limits.maxTokens,
        reasoning: input.definition.reasoningLevel
          ? { effort: input.definition.reasoningLevel }
          : undefined,
      },
      outputType,
    });
    const runner = new Runner({
      modelProvider: budgetedModelProvider(
        input.limits.maxTokens, usedTokens, input.executionEnvelope.executionId, rounds, guardedObserver, answerStream,
        input.answerStream && input.tools.length === 0 ? input.answerStream.fieldPath : undefined,
      ),
      tracingDisabled: true,
      traceIncludeSensitiveData: false,
    });
    const runStartedAt = performance.now();
    const runInput = [...priorHistory, { role: "user", content: input.input.userInput }] as never;
    const runOptions = { context: input.executionEnvelope, maxTurns: input.limits.maxIterations, signal: runSignal };
    const result = observer
      ? await (async () => {
          const streamed = await runner.run(agent, runInput, { ...runOptions, stream: true });
          // Raw SDK events (text, reasoning, tool data) are consumed and discarded; stages come from the owned wrappers.
          for await (const event of streamed) void event;
          await streamed.completed;
          if (streamed.error) throw streamed.error;
          return streamed;
        })()
      : await runner.run(agent, runInput, runOptions);
    // The provisional text was streamed from output that was not one well-formed object: the run cannot vouch for it.
    if (answerStream?.inconsistent) throw new Error(AgentRunFailure.ProviderFailure);
    const usage = result.runContext.usage;
    console.info("Agent run completed", {
      executionId: input.executionEnvelope.executionId,
      durationMs: Math.round(performance.now() - runStartedAt),
      modelCalls: result.rawResponses?.length ?? 1,
      toolCalls: toolCallsUsed,
      inputTokens: usage?.inputTokens ?? 0,
      outputTokens: usage?.outputTokens ?? 0,
    });
    const history = JSON.stringify(result.history);
    if (Buffer.byteLength(history, "utf8") > 1024 * 1024)
      throw new Error(AgentRunFailure.InvalidHistory);
    return {
      isSuccess: true,
      output: typeof result.finalOutput === "string" ? result.finalOutput : JSON.stringify(result.finalOutput),
      structuredOutput: result.finalOutput,
      usage: { inputTokens: usage?.inputTokens ?? 0, outputTokens: usage?.outputTokens ?? 0 },
      iterations: result.rawResponses?.length ?? 1,
      toolCalls: toolCallsUsed,
      toolExecutions: executions,
      history,
    };
  } catch (error) {
    const limit = error instanceof AgentRuntimeLimitError ? error : terminalLimit;
    const code = limit
      ? limit.code
      : error instanceof Error && error.message === AgentRunFailure.InvalidHistory
        ? AgentRunFailure.InvalidHistory
      : timeoutController.signal.aborted
        ? AgentRunFailure.Timeout
        : AgentRunFailure.ProviderFailure;
    console.error("Intelligence agent runtime run failed", {
      executionId: input.executionEnvelope.executionId,
      code,
      providerStatus: isRecord(error) && typeof error.status === "number" ? error.status : undefined,
      providerCode: isRecord(error) && typeof error.code === "string" && /^[a-z_]{1,80}$/.test(error.code) ? error.code : undefined,
      providerParameter: isRecord(error) && typeof error.param === "string" && /^[A-Za-z0-9_.\[\]]{1,160}$/.test(error.param) ? error.param : undefined,
      toolCalls: toolCallsUsed,
      toolNames: executions.map((execution) => execution.toolName),
    });
    return agentRunFailureBody(code, executions);
  } finally {
    clearTimeout(timeout);
  }
}

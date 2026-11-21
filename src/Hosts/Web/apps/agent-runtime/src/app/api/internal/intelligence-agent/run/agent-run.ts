import { Agent, OpenAIProvider, Runner, tool, type Model, type ModelProvider, type StreamEvent } from "@openai/agents";
import { NextRequest } from "next/server";
import { AGENT_CREDENTIAL_HEADER } from "../credential-header";
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
}

const reasoningLevels = new Set(["none", "minimal", "low", "medium", "high", "xhigh", "max"]);
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
  constructor(readonly code: "ToolCallLimitExceeded" | "TokenLimitExceeded" | "Timeout") {
    super(code);
  }
}

/** Single owner of cumulative token accounting for the buffered and streamed model paths. */
function assertTokenBudgetRemains(maxTokens: number, usedTokens: { value: number }): void {
  if (usedTokens.value >= maxTokens) throw new AgentRuntimeLimitError("TokenLimitExceeded");
}

/** Adds reported usage to the cumulative total; usage the provider did not report is treated as budget exhaustion. */
function recordTokenUsage(maxTokens: number, usedTokens: { value: number }, usage: { totalTokens?: number } | undefined): void {
  const total = usage?.totalTokens;
  if (typeof total !== "number" || !Number.isFinite(total) || total < 0) {
    usedTokens.value = maxTokens;
    throw new AgentRuntimeLimitError("TokenLimitExceeded");
  }
  usedTokens.value += total;
  if (usedTokens.value > maxTokens) throw new AgentRuntimeLimitError("TokenLimitExceeded");
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
  };
}

/**
 * Streamed counterpart of the buffered budget check: each provider request is clamped to the remaining budget,
 * usage is accounted on provider completion, and a stream that ends or is cancelled without reported usage
 * exhausts the budget so later rounds and tools are blocked.
 */
async function* budgetedStream(
  model: Model,
  request: Parameters<Model["getStreamedResponse"]>[0],
  maxTokens: number,
  usedTokens: { value: number },
): AsyncGenerator<StreamEvent> {
  assertTokenBudgetRemains(maxTokens, usedTokens);
  const remaining = maxTokens - usedTokens.value;
  const requested = request.modelSettings?.maxTokens;
  const clamped = {
    ...request,
    modelSettings: { ...request.modelSettings, maxTokens: typeof requested === "number" ? Math.min(requested, remaining) : remaining },
  };
  let accounted = false;
  try {
    for await (const event of model.getStreamedResponse(clamped)) {
      if (event.type === "response_done") {
        accounted = true;
        recordTokenUsage(maxTokens, usedTokens, event.response.usage);
      }
      yield event;
    }
    if (!accounted) recordTokenUsage(maxTokens, usedTokens, undefined);
  } finally {
    // Cancellation or provider failure before completion leaves the spend unknown: block any later round or tool.
    if (!accounted) usedTokens.value = maxTokens;
  }
}

function budgetedModelProvider(
  maxTokens: number,
  usedTokens: { value: number },
  executionId: string,
  observer?: AgentRunObserver,
): ModelProvider {
  const provider = new OpenAIProvider();
  return {
    getModel: (modelName?: string) => {
      const model = provider.getModel(modelName);
      if (model instanceof Promise) {
        return model.then((resolved) => budgetedModel(resolved, maxTokens, usedTokens, executionId, observer));
      }
      return budgetedModel(model, maxTokens, usedTokens, executionId, observer);
    },
  };
}

function budgetedModel(
  model: Model,
  maxTokens: number,
  usedTokens: { value: number },
  executionId: string,
  observer?: AgentRunObserver,
): Model {
  let turn = 0;
  return {
    getResponse: async (request) => {
      assertTokenBudgetRemains(maxTokens, usedTokens);
      turn += 1;
      const startedAt = performance.now();
      const response = await model.getResponse(request);
      // Durations and counts only: prompts, tool arguments and replies are clinical data and are never logged.
      console.info("Agent model call completed", {
        executionId,
        turn,
        durationMs: Math.round(performance.now() - startedAt),
        inputTokens: response.usage.inputTokens,
        outputTokens: response.usage.outputTokens,
        reasoningTokens: (response.usage.outputTokensDetails ?? []).reduce((total, details) => total + (details.reasoning_tokens ?? 0), 0),
      });
      recordTokenUsage(maxTokens, usedTokens, response.usage);
      return response;
    },
    getStreamedResponse: (request) =>
      (async function* () {
        // A round the budget already refuses is never announced.
        assertTokenBudgetRemains(maxTokens, usedTokens);
        turn += 1;
        await observer?.modelRoundStarted(turn);
        yield* budgetedStream(model, request, maxTokens, usedTokens);
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
      input.limits?.maxToolCalls < 0 || input.limits?.maxTokens < 1) {
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

type AgentRunFailureCode = AgentRuntimeLimitError["code"] | "InvalidHistory" | "ProviderFailure";

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
    error: {
      code: code === "ToolCallLimitExceeded" ? 5 : code === "TokenLimitExceeded" ? 6 : code === "Timeout" ? 7 : code === "InvalidHistory" ? 11 : 10,
      message: code === "ToolCallLimitExceeded"
        ? "The agent tool-call limit was exceeded."
        : code === "TokenLimitExceeded"
          ? "The agent token limit was exceeded."
          : code === "Timeout"
            ? "The agent execution timed out."
            : code === "InvalidHistory"
              ? "The agent history is invalid."
              : "The agent runtime failed.",
    },
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
  const timeoutController = new AbortController();
  const timeout = setTimeout(() => timeoutController.abort(), timeoutMs);
  const runSignal = AbortSignal.any([signal, timeoutController.signal]);
  const guardedObserver = observer && guardObserver(observer, runSignal);
  const tools = input.tools.map((definition) => {
    const parameters = adaptToolParameters(JSON.parse(definition.parametersJsonSchema));

    return tool({
      name: definition.name,
      description: definition.description,
      parameters: parameters.parameters as never,
      strict: true,
      timeoutMs: 120_000,
      execute: async (argumentsValue: unknown, _context: unknown, details: unknown) => {
        argumentsValue = parameters.decodeArguments(argumentsValue);
        const callId = toolCallId(details);
        assertTokenBudgetRemains(input.limits.maxTokens, usedTokens);
        if (toolCallsUsed >= input.limits.maxToolCalls) {
          throw new AgentRuntimeLimitError("ToolCallLimitExceeded");
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
      modelProvider: budgetedModelProvider(input.limits.maxTokens, usedTokens, input.executionEnvelope.executionId, guardedObserver),
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
      throw new Error("InvalidHistory");
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
    const code = error instanceof AgentRuntimeLimitError
      ? error.code
      : error instanceof Error && error.message === "InvalidHistory"
        ? "InvalidHistory"
      : timeoutController.signal.aborted
        ? "Timeout"
        : "ProviderFailure";
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

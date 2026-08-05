import { Agent, OpenAIProvider, Runner, protocol, tool, type AgentInputItem, type Model, type ModelProvider } from "@openai/agents";
import { NextRequest } from "next/server";

interface AgentDefinition {
  model: string;
  instructions: string;
  reasoningLevel?: "none" | "minimal" | "low" | "medium" | "high" | "xhigh" | "max";
  result?: { name: string; jsonSchema: string; strict: boolean } | null;
}

interface AgentToolDefinition {
  name: string;
  description: string;
  parametersJsonSchema: string;
}

interface AgentExecutionContext {
  executionId: string;
  metadata?: Record<string, string> | null;
}

interface AgentRuntimeRequest {
  definition: AgentDefinition;
  input: { userInput: string; history?: string | null };
  tools: AgentToolDefinition[];
  context: AgentExecutionContext;
  limits: {
    maxIterations: number;
    maxToolCalls: number;
    maxTokens: number;
    effectiveTimeout: string;
  };
  callbackUrl: string;
  callbackCredential: string;
}

const maximumHistoryBytes = 1024 * 1024;
const reasoningLevels = new Set(["none", "minimal", "low", "medium", "high", "xhigh", "max"]);

export function parseHistory(history: string | null | undefined): AgentInputItem[] {
  if (!history) return [];
  if (Buffer.byteLength(history, "utf8") > maximumHistoryBytes)
    throw new Error("InvalidHistory");
  let parsed: unknown;
  try {
    parsed = JSON.parse(history);
  } catch {
    throw new Error("InvalidHistory");
  }
  if (!Array.isArray(parsed)) throw new Error("InvalidHistory");
  const items: AgentInputItem[] = [];
  for (const item of parsed) {
    const validated = protocol.ModelItem.safeParse(item);
    if (!validated.success) throw new Error("InvalidHistory");
    items.push(validated.data);
  }
  return items;
}

interface ToolExecution {
  toolName: string;
  toolCallId: string;
  output: string;
  isError: boolean;
}

class AgentRuntimeLimitError extends Error {
  constructor(readonly code: "ToolCallLimitExceeded" | "TokenLimitExceeded" | "Timeout") {
    super(code);
  }
}

function budgetedModelProvider(maxTokens: number, usedTokens: { value: number }): ModelProvider {
  const provider = new OpenAIProvider();
  return {
    getModel: (modelName?: string) => {
      const model = provider.getModel(modelName);
      if (model instanceof Promise) {
        return model.then((resolved) => budgetedModel(resolved, maxTokens, usedTokens));
      }
      return budgetedModel(model, maxTokens, usedTokens);
    },
  };
}

function budgetedModel(model: Model, maxTokens: number, usedTokens: { value: number }): Model {
  return {
    getResponse: async (request) => {
      if (usedTokens.value >= maxTokens) throw new AgentRuntimeLimitError("TokenLimitExceeded");
      const response = await model.getResponse(request);
      usedTokens.value += response.usage.totalTokens;
      if (usedTokens.value > maxTokens) throw new AgentRuntimeLimitError("TokenLimitExceeded");
      return response;
    },
    getStreamedResponse: (request) => model.getStreamedResponse(request),
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

export async function POST(request: NextRequest) {
  if (!isAuthorized(request)) return Response.json({ error: "Unauthorized" }, { status: 401 });

  let input: AgentRuntimeRequest;
  try {
    input = (await request.json()) as AgentRuntimeRequest;
  } catch {
    return Response.json({ error: "Invalid JSON request." }, { status: 400 });
  }

  if (!input?.definition?.model || !input.definition.instructions || !input.input?.userInput ||
      !input.context?.executionId || !input.callbackUrl || !input.callbackCredential ||
      !Array.isArray(input.tools) || input.limits?.maxIterations < 1 ||
      input.limits?.maxToolCalls < 1 || input.limits?.maxTokens < 1) {
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

  const executions: ToolExecution[] = [];
  let toolCallsUsed = 0;
  const usedTokens = { value: 0 };
  const timeoutController = new AbortController();
  const timeout = setTimeout(() => timeoutController.abort(), timeoutMs);
  const runSignal = AbortSignal.any([request.signal, timeoutController.signal]);
  const tools = input.tools.map((definition) => {
    const schema: unknown = JSON.parse(definition.parametersJsonSchema);

    return tool({
      name: definition.name,
      description: definition.description,
      parameters: schema as never,
      strict: true,
      timeoutMs: 120_000,
      execute: async (argumentsValue: unknown, _context: unknown, details: unknown) => {
        const callId = toolCallId(details);
        if (toolCallsUsed >= input.limits.maxToolCalls) {
          throw new AgentRuntimeLimitError("ToolCallLimitExceeded");
        }
        toolCallsUsed += 1;
        const location = callbackLocation(input.callbackUrl);
        console.info("Calling supplied agent tool callback", {
          executionId: input.context.executionId,
          toolName: definition.name,
          callback: location,
        });
        let response: Response;
        try {
          response = await fetch(input.callbackUrl, {
            method: "POST",
            headers: {
              "Content-Type": "application/json",
              "X-Vitarish-Agent-Credential": input.callbackCredential,
            },
            body: JSON.stringify({
              toolCallId: callId,
              toolName: definition.name,
              argumentsJson: JSON.stringify(argumentsValue),
              executionId: input.context.executionId,
              metadata: input.context.metadata ?? {},
            }),
            signal: AbortSignal.any([runSignal, AbortSignal.timeout(120_000)]),
          });
        } catch (error) {
          const detail = error instanceof Error ? error : new Error(String(error));
          console.error("Supplied agent tool callback fetch failed", {
            executionId: input.context.executionId,
            toolName: definition.name,
            callback: location,
            errorName: detail.name,
            errorMessage: detail.message,
            cause: detail.cause instanceof Error ? `${detail.cause.name}: ${detail.cause.message}` : String(detail.cause ?? ""),
          });
          throw error;
        }

        if (!response.ok) {
          const message = `Clinic tool ${definition.name} returned HTTP ${response.status}.`;
          console.error("Supplied agent tool callback returned failure", {
            executionId: input.context.executionId,
            toolName: definition.name,
            callback: location,
            status: response.status,
          });
          executions.push({ toolName: definition.name, toolCallId: callId, output: message, isError: true });
          return message;
        }

        const result = (await response.json()) as { callId: string; output: string; isError: boolean };
        executions.push({
          toolName: definition.name,
          toolCallId: result.callId,
          output: result.output,
          isError: result.isError,
        });
        console.info("Supplied agent tool callback succeeded", {
          executionId: input.context.executionId,
          toolName: definition.name,
          status: response.status,
          isToolError: result.isError,
        });
        return result.output;
      },
    });
  });

  console.info("Received supplied agent tools", {
    executionId: input.context.executionId,
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
      name: "Vitarish patient workspace agent",
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
      modelProvider: budgetedModelProvider(input.limits.maxTokens, usedTokens),
      tracingDisabled: true,
      traceIncludeSensitiveData: false,
    });
    const result = await runner.run(agent, [
      ...priorHistory,
      { role: "user", content: input.input.userInput },
    ] as never, {
      context: input.context,
      maxTurns: input.limits.maxIterations,
      signal: runSignal,
    });
    const usage = result.runContext.usage;
    const history = JSON.stringify(result.history);
    if (Buffer.byteLength(history, "utf8") > maximumHistoryBytes)
      throw new Error("InvalidHistory");
    return Response.json({
      isSuccess: true,
      output: typeof result.finalOutput === "string" ? result.finalOutput : JSON.stringify(result.finalOutput),
      structuredOutput: result.finalOutput,
      usage: { inputTokens: usage?.inputTokens ?? 0, outputTokens: usage?.outputTokens ?? 0 },
      iterations: result.rawResponses?.length ?? 1,
      toolCalls: toolCallsUsed,
      toolExecutions: executions,
      history,
    });
  } catch (error) {
    const code = error instanceof AgentRuntimeLimitError
      ? error.code
      : error instanceof Error && error.message === "InvalidHistory"
        ? "InvalidHistory"
      : timeoutController.signal.aborted
        ? "Timeout"
        : "ProviderFailure";
    console.error("Intelligence agent runtime run failed", {
      executionId: input.context.executionId,
      code,
      toolCalls: toolCallsUsed,
      toolNames: executions.map((execution) => execution.toolName),
    });
    return Response.json({
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
    });
  } finally {
    clearTimeout(timeout);
  }
}

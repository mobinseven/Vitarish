import { randomUUID } from "node:crypto";
import { once } from "node:events";
import { closeSync, fsyncSync, openSync, readFileSync, unlinkSync, writeSync } from "node:fs";
import { createServer } from "node:http";
import { join } from "node:path";

export const benchmarkModel = "gpt-6-luna";
const responsesPath = "/v1/responses";
const providerUrl = new URL(responsesPath, "https://api.openai.com");
const tokenCountUrl = new URL("responses/input_tokens", new URL("/v1/", providerUrl));
export const benchmarkCap = 2_000_000;
const inputLimit = 1_050_000;
const outputLimit = 128_000;
const shortContextLimit = 272_000;
const maxBodyBytes = 8 * 1024 * 1024;

// Upper bounds in quarter-microdollars per token. Use the long-context,
// extended cache-write rate for all input and the long-context output rate.
const inputRate = 2;
const outputRate = 3;
const cost = (input, output) => Math.ceil((input * inputRate + output * outputRate) / 4);
const requestCost = (input, output) => input <= shortContextLimit
  ? Math.ceil((input + 2 * output) / 4) : cost(input, output);
const within = (value, limit) => Number.isSafeInteger(value) && value >= 0 && value <= limit;

export function openLedger(directory, cap = benchmarkCap) {
  if (!within(cap, benchmarkCap) || cap === 0) throw new Error("Invalid benchmark cap.");
  const journal = join(directory, "provider-budget.jsonl");
  const lock = join(directory, "provider-budget.lock");
  const lockFd = openSync(lock, "wx", 0o600);
  let fd;
  const pending = new Map();
  let charged = 0;
  let reserved = 0;
  let unavailable = false;

  function apply(entry) {
    if (!within(entry.amount, cap) || typeof entry.id !== "string") throw new Error("Invalid budget record.");
    if (entry.kind === "reserve" && !pending.has(entry.id) && entry.amount > 0 &&
        entry.amount <= cap - charged - reserved) {
      pending.set(entry.id, entry.amount);
      reserved += entry.amount;
    } else if (entry.kind === "settle" && pending.has(entry.id) && entry.amount <= pending.get(entry.id)) {
      reserved -= pending.get(entry.id);
      pending.delete(entry.id);
      charged += entry.amount;
    } else {
      throw new Error("Inconsistent budget record.");
    }
  }

  try {
    fd = openSync(journal, "a+", 0o600);
    const entries = readFileSync(fd, "utf8");
    if (entries && !entries.endsWith("\n")) throw new Error("Incomplete budget journal.");
    for (const line of entries.trimEnd().split("\n").filter(Boolean)) apply(JSON.parse(line));
  } catch {
    if (fd !== undefined) closeSync(fd);
    closeSync(lockFd);
    unlinkSync(lock);
    throw new Error("Budget journal could not be opened safely.");
  }

  function record(entry) {
    if (unavailable) throw new Error("Budget journal unavailable.");
    try {
      const data = Buffer.from(`${JSON.stringify(entry)}\n`);
      if (writeSync(fd, data) !== data.length) throw new Error("Incomplete budget write.");
      fsyncSync(fd);
      apply(entry);
    } catch {
      unavailable = true;
      throw new Error("Budget journal could not record the request safely.");
    }
  }

  return {
    snapshot: () => ({ capMicrousd: cap, chargedUpperBoundMicrousd: charged, unresolvedMicrousd: reserved, unavailable }),
    reserve(amount) {
      if (amount > cap - charged - reserved) return null;
      const id = randomUUID();
      record({ kind: "reserve", id, amount });
      return id;
    },
    settle(id, amount) { record({ kind: "settle", id, amount }); },
    close() {
      closeSync(fd);
      closeSync(lockFd);
      unlinkSync(lock);
    },
  };
}

async function readJson(body) {
  const chunks = [];
  let total = 0;
  for await (const chunk of body) {
    total += chunk.length;
    if (total > maxBodyBytes) throw new Error("Body limit exceeded.");
    chunks.push(Buffer.from(chunk));
  }
  return JSON.parse(new TextDecoder("utf-8", { fatal: true }).decode(Buffer.concat(chunks)));
}

function isInlineText(value) {
  return typeof value === "string" || Array.isArray(value) && value.every((item) =>
    item?.type === "input_text" || item?.type === "output_text" || item?.type === "refusal");
}

function isLocalInput(item) {
  return (item?.type === "message" || item?.type === undefined) && isInlineText(item?.content) ||
    (item?.type === "function_call_output" && typeof item.output === "string") ||
    (item?.type === "function_call" && typeof item.arguments === "string") ||
    (item?.type === "reasoning" && typeof item.encrypted_content === "string");
}

function isBenchmarkRequest(input) {
  return input?.model === benchmarkModel && within(input.max_output_tokens, outputLimit) &&
    input.max_output_tokens > 0 && (!input.tools || Array.isArray(input.tools) &&
      input.tools.every((tool) => tool?.type === "function")) &&
    (typeof input.input === "string" || Array.isArray(input.input) && input.input.every(isLocalInput)) &&
    (!input.service_tier || input.service_tier === "default") &&
    !["background", "previous_response_id", "conversation", "prompt"].some((key) => input[key]);
}

function actualBound(result, countedInput, requestedOutput) {
  const usage = result?.usage;
  if (result?.model === benchmarkModel && within(usage?.input_tokens, countedInput) &&
      within(usage.output_tokens, requestedOutput) &&
      usage.total_tokens === usage.input_tokens + usage.output_tokens) {
    if (usage.input_tokens > shortContextLimit) return cost(usage.input_tokens, usage.output_tokens);
    const reportedCache = usage.input_tokens_details?.cached_tokens;
    const cached = within(reportedCache, usage.input_tokens) ? reportedCache : 0;
    // Use the highest short-context cache write rate for unseen input,
    // the published cache read rate for confirmed cache hits, and round up.
    return Math.ceil(((usage.input_tokens - cached) * 25 + cached + usage.output_tokens * 50) / 100);
  }
  return null;
}

async function countInput(send, options, input) {
  const { model, input: messages, instructions, parallel_tool_calls, personality, reasoning, text, tool_choice, tools, truncation } = input;
  const response = await send(tokenCountUrl, {
    ...options, body: JSON.stringify({ model, input: messages, instructions, parallel_tool_calls, personality, reasoning, text, tool_choice, tools, truncation }),
  });
  if (!response.ok || !response.body) {
    await response.body?.cancel();
    throw new Error("Provider token count unavailable.");
  }
  const result = await readJson(response.body);
  if (!within(result?.input_tokens, inputLimit) || result.input_tokens === 0) {
    throw new Error("Provider token count was invalid.");
  }
  return result.input_tokens;
}

function sendJson(response, status, value) {
  response.writeHead(status, { "content-type": "application/json" });
  response.end(JSON.stringify(value));
}

async function relayStream(body, response, signal) {
  const decoder = new TextDecoder("utf-8", { fatal: true });
  let pending = "";
  let terminal;
  let terminalCount = 0;
  for await (const chunk of body) {
    pending += decoder.decode(chunk, { stream: true });
    let newline = pending.indexOf("\n");
    while (newline >= 0) {
      const line = pending.slice(0, newline).trimEnd();
      if (line.length > maxBodyBytes) throw new Error("Provider event exceeds limit.");
      pending = pending.slice(newline + 1);
      if (line.startsWith("data:")) {
        const content = line.slice(5).trim();
        if (content && content !== "[DONE]") {
          const event = JSON.parse(content);
          if (["response.completed", "response.incomplete", "response.failed"].includes(event.type)) {
            terminalCount++;
            terminal = event.response;
          }
        }
      }
      newline = pending.indexOf("\n");
    }
    if (pending.length > maxBodyBytes) throw new Error("Provider event exceeds limit.");
    if (!response.write(chunk)) await once(response, "drain", { signal });
  }
  return terminalCount === 1 && !(pending + decoder.decode()).trim() ? terminal : null;
}

export function createGateway({ ledger, apiKey, send = fetch }) {
  return createServer(async (request, response) => {
    if (request.method === "GET" && request.url === "/budget") {
      sendJson(response, 200, ledger.snapshot());
      return;
    }
    if (request.method !== "POST" || request.url !== responsesPath) {
      sendJson(response, 404, { error: { message: "Benchmark route unavailable." } });
      return;
    }
    let input;
    try { input = await readJson(request); } catch {
      sendJson(response, 400, { error: { message: "Invalid benchmark request body." } });
      return;
    }
    if (!isBenchmarkRequest(input)) {
      sendJson(response, 400, { error: { message: "Request is outside the benchmark billing scope." } });
      return;
    }

    const disconnected = new AbortController();
    response.once("close", () => disconnected.abort());
    const signal = AbortSignal.any([disconnected.signal, AbortSignal.timeout(180_000)]);
    try {
      const requestOptions = {
        method: "POST", redirect: "error", signal,
        headers: { authorization: `Bearer ${apiKey}`, "content-type": "application/json" },
      };
      const countedInput = await countInput(send, requestOptions, input);
      if (signal.aborted) return;
      const id = ledger.reserve(requestCost(countedInput, input.max_output_tokens));
      if (!id) {
        sendJson(response, 402, { error: { message: "Benchmark budget exhausted." } });
        return;
      }
      const upstream = await send(providerUrl, {
        ...requestOptions, body: JSON.stringify({ ...input, service_tier: "default" }),
      });
      if (!upstream.ok || !upstream.body) {
        await upstream.body?.cancel();
        sendJson(response, 502, { error: { message: "Benchmark provider did not complete." } });
        return;
      }
      const mediaType = upstream.headers.get("content-type")?.split(";")[0].trim();
      let result;
      let buffered;
      if (input.stream && mediaType === "text/event-stream") {
        response.writeHead(200, { "content-type": mediaType });
        result = await relayStream(upstream.body, response, signal);
      } else if (!input.stream && mediaType === "application/json") {
        result = await readJson(upstream.body);
        buffered = JSON.stringify(result);
        response.writeHead(200, { "content-type": mediaType });
      } else {
        await upstream.body.cancel();
        throw new Error("Unexpected provider media type.");
      }
      const amount = actualBound(result, countedInput, input.max_output_tokens);
      if (amount !== null && !signal.aborted) ledger.settle(id, amount);
      response.end(buffered);
    } catch {
      if (response.headersSent || response.destroyed) response.destroy();
      else sendJson(response, 502, { error: { message: "Benchmark provider outcome is uncertain." } });
    }
  });
}

if (import.meta.main) {
  const [directory] = process.argv.slice(2);
  if (!directory || !process.env.OPENAI_API_KEY) throw new Error("Benchmark ledger directory and provider credential are required.");
  const ledger = openLedger(directory);
  const server = createGateway({ ledger, apiKey: process.env.OPENAI_API_KEY });
  // This process runs inside the benchmark network with no published ports.
  server.listen(4319, "0.0.0.0");
  for (const signal of ["SIGINT", "SIGTERM"]) process.once(signal, () => server.close(() => ledger.close()));
}

import { execFileSync } from "node:child_process";
import { readFile } from "node:fs/promises";
import { lookup } from "node:dns/promises";
import authentication from "../shared/auth/src/api/specs/Main_authentication.json" with { type: "json" };
import intelligence from "../shared/intelligence/src/api/specs/Main_intelligence.json" with { type: "json" };
import clinic from "../src/api/specs/Main_clinic.json" with { type: "json" };
import { TENANT_HOST_HEADER } from "../shared/api-client/src/tenantHostHeader.ts";
import { REQUEST_ID_HEADER } from "../shared/api-client/src/requestIdHeader.ts";
import { readAgentTurnFrames } from "../shared/intelligence/src/api/intelligence/agentTurnFrames.ts";
import { benchmarkCap, benchmarkModel } from "./agent-benchmark-provider-gateway.mjs";

const json = "application/json";
const timeoutMs = 180_000;
const maxJsonBytes = 4 * 1024 * 1024;

// Resolve each path and media type from its unique successful response in the checked-in contract.
function operation(spec, method, schema) {
  const ref = `#/components/schemas/${schema}`;
  const matches = Object.entries(spec.paths).flatMap(([path, item]) =>
    Object.entries(item[method]?.responses?.["200"]?.content ?? {})
      .filter(([, media]) => media.schema?.$ref === ref)
      .map(([mediaType]) => ({ path, method, mediaType })),
  );
  if (matches.length !== 1) throw new Error(`Expected one OpenAPI operation for ${schema}.`);
  return matches[0];
}

export const operations = {
  antiforgery: operation(authentication, "get", "AntiforgeryTokenDto"),
  capabilities: operation(intelligence, "get", "AgentConversationCapabilitiesResponse"),
  buffered: operation(intelligence, "post", "RunAgentConversationResponse"),
  streamed: operation(intelligence, "post", "IntelligenceStreamChunkOfAgentTurnPayload"),
  shortcut: operation(clinic, "get", "TodayAppointmentCountResponse"),
};

const scenarios = {
  appointmentCount: {
    en: "How many active booked appointments are there today?",
    fa: "امروز چند نوبت فعال ثبت شده است؟",
  },
  dependentLookup: {
    en: "Which services with published shifts have available appointments tomorrow? Check availability and report the exact stored service names and capacity. Do not make changes.",
    fa: "فردا کدام خدمات با شیفت منتشرشده نوبت خالی دارند؟ ظرفیت و نام دقیق ثبت‌شدهٔ خدمات را گزارش کن و هیچ تغییری نده.",
  },
  shiftProposal: {
    en: "Prepare a public shift proposal for next Tuesday from 09:00 to 11:00 with 6 seats. Show the proposal for my review without creating or publishing the shift.",
    fa: "پیشنهاد یک شیفت عمومی برای سه‌شنبهٔ آینده از ساعت ۹ تا ۱۱ با ظرفیت ۶ نفر آماده کن. برای بررسی نشان بده و شیفت را ایجاد یا منتشر نکن.",
  },
  help: {
    en: "How do I book a patient into an existing shift?",
    fa: "چطور برای بیمار در یک شیفت موجود نوبت ثبت کنم؟",
  },
};

function hasMediaType(response, expected) {
  return response.headers.get("content-type")?.split(";")[0].trim().toLowerCase() === expected;
}

async function readJson(response) {
  if (!hasMediaType(response, json) || !response.body) throw new Error("Expected a JSON response.");
  const reader = response.body.getReader();
  const decoder = new TextDecoder("utf-8", { fatal: true });
  let bytes = 0;
  let body = "";
  try {
    for (;;) {
      const { done, value } = await reader.read();
      if (done) return JSON.parse(body + decoder.decode());
      bytes += value.byteLength;
      if (bytes > maxJsonBytes) throw new Error("JSON response exceeded the benchmark limit.");
      body += decoder.decode(value, { stream: true });
    }
  } finally {
    await reader.cancel().catch(() => undefined);
  }
}

function isTurnResult(value) {
  return value && typeof value.agentRunId === "string" && value.agentRunId.length > 0 &&
    typeof value.conversationId === "string" && value.conversationId.length > 0 &&
    value.result && typeof value.result === "object" && !Array.isArray(value.result);
}

function executionId(value) {
  return typeof value === "string" && /^(?:[a-f0-9]{32}|[a-f0-9]{8}(?:-[a-f0-9]{4}){3}-[a-f0-9]{12})$/i.test(value)
    ? value : null;
}

function verifyResult(payload, sample, expectation) {
  if (sample.transport === "shortcut") return payload.count === expectation.appointmentCount;
  const { messages, actions } = payload.result;
  if (!Array.isArray(messages) || !Array.isArray(actions)) return false;
  const text = messages.map((message) => typeof message?.content === "string" ? message.content : "")
    .join(" ").toLocaleLowerCase(sample.locale);
  if (sample.scenario === "shiftProposal") {
    return actions.length === 1 && actions[0]?.actionType === expectation.shiftAction;
  }
  if (actions.length !== 0) return false;
  const expected = sample.scenario === "dependentLookup" ? [expectation.serviceName]
    : sample.scenario === "help" ? expectation.helpTerms[sample.locale] : expectation.countTerms[sample.locale];
  return expected.some((term) => text.includes(term.toLocaleLowerCase(sample.locale)));
}

function canStream(capabilities, route) {
  return Array.isArray(capabilities?.agents) && capabilities.agents.some(
    (agent) => agent?.agentKey === route.agentKey && agent.subjectKind === route.subjectKind && agent.streamingEnabled === true,
  );
}

function createClient(origin, sessionCookie, send) {
  const cookies = new Map(sessionCookie.split(";").map((part) => {
    const separator = part.indexOf("=");
    if (separator < 1) throw new Error("Invalid benchmark session cookie.");
    return [part.slice(0, separator).trim(), part.slice(separator + 1).trim()];
  }));
  let antiforgery;
  return async (op, locale, body) => {
    const url = new URL(op.path, origin);
    if (url.origin !== origin.origin) throw new Error("Unexpected benchmark request origin.");
    const headers = new Headers({
      accept: op.mediaType,
      "accept-language": locale,
      cookie: Array.from(cookies, ([name, value]) => `${name}=${value}`).join("; "),
      [TENANT_HOST_HEADER]: origin.host,
    });
    if (body) {
      headers.set("origin", origin.origin);
      headers.set(REQUEST_ID_HEADER, crypto.randomUUID());
      headers.set("content-type", json);
      headers.set(antiforgery.headerName, antiforgery.requestToken);
    }
    const response = await send(url, {
      method: op.method, headers, redirect: "error", signal: AbortSignal.timeout(timeoutMs),
      body: body ? JSON.stringify(body) : undefined,
    });
    for (const cookie of response.headers.getSetCookie()) {
      const [pair, ...attributes] = cookie.split(";");
      const separator = pair.indexOf("=");
      if (separator < 1) continue;
      const name = pair.slice(0, separator);
      const expired = attributes.some((attribute) => {
        const expiry = /^\s*expires=(.+)$/i.exec(attribute);
        return /^\s*max-age=0\s*$/i.test(attribute) || (expiry && Date.parse(expiry[1]) <= Date.now());
      });
      if (expired) cookies.delete(name);
      else if (!attributes.some((attribute) => /^\s*path=(?!\/\s*$)/i.test(attribute))) {
        cookies.set(name, pair.slice(separator + 1));
      }
    }
    if (op === operations.antiforgery) {
      if (!response.ok) throw new Error("Antiforgery bootstrap failed.");
      antiforgery = await readJson(response);
      if (typeof antiforgery?.headerName !== "string" || !antiforgery.headerName ||
          typeof antiforgery.requestToken !== "string" || !antiforgery.requestToken ||
          headers.has(antiforgery.headerName) ||
          ["origin", "content-type"].includes(antiforgery.headerName.toLowerCase())) {
        throw new Error("Invalid antiforgery response.");
      }
    }
    return response;
  };
}

async function measure(request, sample, clock, expectation) {
  const start = clock();
  const elapsed = () => Math.round((clock() - start) * 100) / 100;
  const result = {
    ...sample, outcome: "unknown", status: null, observedMs: null, executionId: null, correct: null,
    firstAcceptedMs: null, firstProgressMs: null, firstTextMs: null, terminalMs: null,
  };
  try {
    const response = await request();
    result.status = response.status;
    if (!response.ok) {
      // A server error can happen after acceptance or commit.
      result.outcome = response.status >= 400 && response.status < 500 && response.status !== 408 ? "http-error" : "unknown";
      await response.body?.cancel();
    } else if (sample.transport === "streamed") {
      if (!response.body || !hasMediaType(response, operations.streamed.mediaType)) {
        await response.body?.cancel();
        return result;
      }
      let terminal;
      for await (const frame of readAgentTurnFrames(response.body)) {
        if (frame.kind === "started") result.firstAcceptedMs = elapsed();
        if (frame.kind === "progress") result.firstProgressMs ??= elapsed();
        if (frame.kind === "delta" && frame.text.trim()) result.firstTextMs ??= elapsed();
        if (frame.kind === "completed" || frame.kind === "failed" || frame.kind === "canceled") {
          terminal = { kind: frame.kind, ms: elapsed(), execution: executionId(frame.response?.agentRunId),
            correct: frame.kind === "completed" && expectation ? verifyResult(frame.response, sample, expectation) : null };
        }
      }
      result.outcome = terminal.kind;
      result.terminalMs = terminal.ms;
      result.executionId = terminal.execution;
      result.correct = terminal.correct;
    } else {
      const payload = await readJson(response);
      const valid = sample.transport === "shortcut"
        ? Number.isSafeInteger(payload?.count) && payload.count >= 0
        : isTurnResult(payload);
      if (valid) {
        result.outcome = "completed";
        result.terminalMs = elapsed();
        if (expectation) result.correct = verifyResult(payload, sample, expectation);
        if (sample.transport === "buffered") result.executionId = executionId(payload.agentRunId);
        if (sample.scenario === "help" && sample.transport === "buffered" &&
            Array.isArray(payload.result.messages) && payload.result.messages.some(
              (message) => typeof message?.content === "string" && message.content.trim(),
            )) {
          result.firstTextMs = result.terminalMs;
        }
      }
    }
  } catch {
    // Network errors, deadline, malformed frames and EOF without terminal all leave an uncertain outcome.
    // Keep only the redacted outcome in the report, never the thrown error or response body.
  } finally {
    result.observedMs = elapsed();
  }
  return result;
}

function summarize(samples) {
  const groups = Map.groupBy(samples, ({ scenario, locale, transport }) => `${scenario}/${locale}/${transport}`);
  return Object.fromEntries(Array.from(groups, ([name, group]) => {
    const timings = (field) => {
      const values = group.filter(({ outcome }) => outcome === "completed")
        .map((sample) => sample[field]).filter(Number.isFinite).sort((a, b) => a - b);
      const percentile = (p) => values[Math.ceil(p * values.length) - 1] ?? null;
      return { count: values.length, p50: percentile(0.5), p95: percentile(0.95) };
    };
    return [name, {
      attempted: group.length,
      outcomes: Object.fromEntries(Array.from(Map.groupBy(group, (sample) => sample.outcome),
        ([outcome, rows]) => [outcome, rows.length])),
      firstAcceptedMs: timings("firstAcceptedMs"),
      firstProgressMs: timings("firstProgressMs"),
      firstTextMs: timings("firstTextMs"),
      terminalMs: timings("terminalMs"),
    }];
  }));
}

const sampleKey = ({ scenario, locale, pair, transport }) => `${pair}/${scenario}/${locale}/${transport}`;

export async function benchmark(options, send = fetch, clock = () => performance.now()) {
  const { origin, sessionCookie, fixture, pairs, capMicrousd, pairCeilingMicrousd, getBudget, onSample, comparisonPhase, previous } = options;
  const locales = Object.keys(scenarios.help);
  const client = createClient(origin, sessionCookie, send);
  const samples = [...(previous?.samples ?? [])];
  const completed = new Set(samples.map(sampleKey));
  let reservedMicrousd = 0;
  let stop = "finished";

  await client(operations.antiforgery, locales[0]);
  const response = await client(operations.capabilities, locales[0]);
  if (!response.ok) {
    await response.body?.cancel();
    throw new Error("Capability preflight failed.");
  }
  const capabilities = await readJson(response);
  if (!canStream(capabilities, fixture.practice) || !canStream(capabilities, fixture.help)) {
    throw new Error("Both benchmark routes must advertise streaming before sending turns.");
  }

  paired: for (let pair = 1; pair <= pairs; pair++) {
    for (const [scenario, prompt] of Object.entries(scenarios)) {
      for (const locale of locales) {
        const order = pair % 2 === 1 ? ["buffered", "streamed"] : ["streamed", "buffered"];
        const missing = order.map((transport) => ({ scenario, locale, pair, transport }))
          .filter((sample) => !completed.has(sampleKey(sample)));
        if (missing.length === 0) continue;
        const budget = getBudget ? await getBudget() : null;
        if (budget
          ? budget.unavailable || budget.chargedUpperBoundMicrousd + budget.unresolvedMicrousd >= capMicrousd
          : pairCeilingMicrousd > capMicrousd - reservedMicrousd) {
          stop = "budget";
          break paired;
        }
        if (!budget) reservedMicrousd += pairCeilingMicrousd;
        const route = scenario === "help" ? fixture.help : fixture.practice;
        for (const planned of missing) {
          const sample = await measure(
            () => client(operations[planned.transport], locale, { ...route, message: prompt[locale] }),
            planned, clock, fixture.expectation,
          );
          samples.push(sample);
          onSample?.(sample);
          if (sample.correct === false && comparisonPhase !== "baseline") {
            stop = "incorrect-result";
            break paired;
          }
          if (sample.outcome !== "completed") {
            stop = sample.outcome === "unknown" ? "uncertain-outcome" : "turn-failed";
            break paired;
          }
        }
      }
    }
  }

  if (stop === "finished") {
    for (const locale of locales) {
      samples.push(await measure(() => client(operations.shortcut, locale),
        { scenario: "appointmentCount", locale, pair: null, transport: "shortcut" }, clock, fixture.expectation));
    }
  }
  return {
    stop, comparisonPhase, requestedPairsPerCohort: pairs, capMicrousd, reservedMicrousd, actualSpendMicrousd: null,
    rejectedAttempts: previous?.rejectedAttempts ?? [],
    ...(getBudget ? { providerBudget: await getBudget() } : {}),
    samples, cohorts: summarize(samples),
  };
}
const sessionCookieEnvironmentKey = "VITARISH_AGENT_BENCHMARK_SESSION_COOKIE";

async function runCli() {
  const [configPath, ...extra] = process.argv.slice(2);
  if (!configPath || extra.length) {
    process.stderr.write("Usage: node scripts/agent-turn-benchmark.mjs <approved-test-config.json>\n");
    process.exitCode = 2;
    return;
  }
  const config = JSON.parse(await readFile(configPath, "utf8"));
  const origin = new URL(config.origin);
  const addresses = await lookup(origin.hostname.replace(/^\[|\]$/g, ""), { all: true });
  const isValidOrigin = origin.origin === config.origin && addresses.length > 0 &&
    addresses.every(({ address }) => address.startsWith("127.") || address === "::1") &&
    (origin.protocol === "https:" || origin.protocol === "http:");
  const positiveInteger = (value) => Number.isSafeInteger(value) && value > 0;
  const label = (value) => typeof value === "string" && /^[\w .:/()+-]{1,100}$/.test(value);
  const route = (value) => value && [value.agentKey, value.subjectKind, value.subjectId].every(label) &&
    Object.keys(value).length === 3;
  const expectation = config.fixture?.expectation;
  const terms = (value) => Object.keys(scenarios.help).every((locale) =>
    Array.isArray(value?.[locale]) && value[locale].length > 0 &&
    value[locale].every((term) => typeof term === "string" && term.trim().length > 0 && term.length <= 160));
  const sessionCookie = process.env[sessionCookieEnvironmentKey];
  if (config.syntheticPractice !== true || !isValidOrigin ||
      typeof config.gatewayContainer !== "string" || !/^[a-zA-Z0-9][a-zA-Z0-9_-]{0,79}$/.test(config.gatewayContainer) || !sessionCookie ||
      !positiveInteger(config.pairs) || !["baseline", "candidate"].includes(config.comparisonPhase) ||
      config.capMicrousd !== benchmarkCap || config.model !== benchmarkModel ||
      ![config.fixture?.name, config.provider, config.model, config.modelSettings].every(label) ||
      !route(config.fixture.practice) || !route(config.fixture.help) ||
      !Number.isSafeInteger(expectation?.appointmentCount) || expectation.appointmentCount < 0 ||
      !label(expectation.serviceName) || !label(expectation.shiftAction) ||
      !terms(expectation.countTerms) || !terms(expectation.helpTerms) ||
      config.fixture.practice.agentKey === config.fixture.help.agentKey) {
    throw new Error("Invalid synthetic benchmark configuration.");
  }
  const getBudget = async () => {
    const status = JSON.parse(execFileSync("docker", ["exec", config.gatewayContainer, "node", "-e",
      "fetch('http://127.0.0.1:4319/budget',{signal:AbortSignal.timeout(3000)}).then(r=>{if(!r.ok)process.exit(1);return r.text()}).then(console.log)",
    ], { encoding: "utf8", timeout: 5_000, stdio: ["ignore", "pipe", "ignore"] }));
    const { capMicrousd, chargedUpperBoundMicrousd, unresolvedMicrousd, unavailable } = status;
    if (capMicrousd !== benchmarkCap || ![chargedUpperBoundMicrousd, unresolvedMicrousd].every(
      (value) => Number.isSafeInteger(value) && value >= 0 && value <= capMicrousd,
    ) || chargedUpperBoundMicrousd + unresolvedMicrousd > capMicrousd || typeof unavailable !== "boolean") {
      throw new Error("Benchmark provider ledger returned invalid accounting.");
    }
    return status;
  };
  const revision = execFileSync("git", ["rev-parse", "HEAD"], { encoding: "utf8" }).trim();
  let previous;
  if (config.resumeReport) {
    const saved = JSON.parse(await readFile(config.resumeReport, "utf8"));
    const excluded = saved.samples.at(-1);
    const samples = saved.samples.slice(0, -1);
    if (saved.mode !== "main" || saved.revision !== revision || saved.fixture !== config.fixture.name ||
        saved.provider !== config.provider || saved.model !== config.model || saved.modelSettings !== config.modelSettings ||
        saved.comparisonPhase !== config.comparisonPhase || saved.requestedPairsPerCohort !== config.pairs ||
        saved.capMicrousd !== config.capMicrousd || saved.mainImage !== config.mainImage ||
        saved.stop !== "turn-failed" || excluded?.outcome !== "http-error" || excluded.status !== 403 ||
        excluded.transport !== "streamed" || excluded.executionId ||
        samples.some((sample) => sample.outcome !== "completed" || sample.transport === "shortcut") ||
        new Set(samples.map(sampleKey)).size !== samples.length) {
      throw new Error("Prior report is not an eligible pre-execution authorization stop.");
    }
    previous = { samples, rejectedAttempts: [...(saved.rejectedAttempts ?? []), excluded] };
  }
  const report = await benchmark({
    ...config, origin, sessionCookie, getBudget, previous,
    onSample: ({ scenario, locale, pair, transport, outcome, correct }) => {
      process.stderr.write(`${pair} ${scenario} ${locale} ${transport} ${outcome} ${correct ? "verified" : "unverified"}\n`);
    },
  });
  process.stdout.write(`${JSON.stringify({
    mode: "main", revision, fixture: config.fixture.name,
    provider: config.provider, model: config.model, modelSettings: config.modelSettings,
    ...report,
  }, null, 2)}\n`);
  if (report.stop !== "finished" || report.samples.some(({ outcome, correct }) => outcome !== "completed" ||
      (config.comparisonPhase === "candidate" && correct !== true))) process.exitCode = 1;
}

if (import.meta.main) {
  runCli().catch(() => {
    // Config, headers and response errors can contain credentials. Preserve a nonzero exit and redact details.
    process.stderr.write("Benchmark setup failed. Check configuration, session, and advertised streaming capabilities.\n");
    process.exitCode = 1;
  });
}

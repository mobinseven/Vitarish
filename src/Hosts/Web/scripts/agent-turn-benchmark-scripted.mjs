import assert from "node:assert/strict";
import { execFileSync } from "node:child_process";
import { randomUUID } from "node:crypto";
import { TENANT_HOST_HEADER } from "../shared/api-client/src/tenantHostHeader.ts";
import { REQUEST_ID_HEADER } from "../shared/api-client/src/requestIdHeader.ts";
import { StreamChunkKindValue as kind } from "../shared/intelligence/src/api/intelligence/agentTurnFrames.ts";
import { benchmark, operations } from "./agent-turn-benchmark.mjs";

const origin = new URL("http://benchmark.localhost");
const fixture = {
  practice: { agentKey: "scripted-operations", subjectKind: "scripted", subjectId: "synthetic" },
  help: { agentKey: "scripted-help", subjectKind: "scripted", subjectId: "synthetic" },
};
const options = { origin, fixture, sessionCookie: "scripted-session=fixture", pairs: 2, capMicrousd: 160, pairCeilingMicrousd: 10 };
const headerName = "X-Scripted-Antiforgery";
const requestToken = "scripted-antiforgery-value";

function scriptedMain({ truncate = false, enabled = true, invalidSequence = false, canceled = false, rotateSession = false } = {}) {
  const calls = [];
  const send = async (url, request) => {
    const entry = Object.entries(operations)
      .find(([, op]) => op.path === url.pathname && op.method === request.method);
    assert.ok(entry);
    const [route, op] = entry;
    const locale = request.headers.get("accept-language");
    assert.equal(url.origin, origin.origin);
    assert.equal(request.redirect, "error");
    assert.equal(request.headers.get(TENANT_HOST_HEADER), origin.host);
    assert.equal(request.headers.get("accept"), op.mediaType);
    const rotated = rotateSession && calls.some(({ route }) => route === "buffered");
    assert.match(request.headers.get("cookie"), rotated ? /scripted-session=renewed/ : /scripted-session=fixture/);
    calls.push({ route, locale });
    if (route === "antiforgery") {
      return Response.json({ headerName, requestToken }, { headers: { "set-cookie": "scripted-guard=fixture; HttpOnly" } });
    }
    assert.match(request.headers.get("cookie"), /scripted-guard=fixture/);
    if (route === "capabilities") {
      return Response.json({ agents: Object.values(fixture).map(({ agentKey, subjectKind }) =>
        ({ agentKey, subjectKind, streamingEnabled: enabled })) });
    }
    if (route === "shortcut") return Response.json({ count: 4 });

    assert.equal(request.headers.get(headerName), requestToken);
    assert.equal(request.headers.get("origin"), origin.origin);
    assert.match(request.headers.get(REQUEST_ID_HEADER), /^[\da-f-]{36}$/i);
    const turn = JSON.parse(request.body);
    assert.equal(turn.conversationId, undefined);
    assert.equal(turn.subjectId, fixture.practice.subjectId);
    calls.at(-1).message = turn.message;
    const help = turn.agentKey === fixture.help.agentKey;
    const answer = locale === "fa" ? "پاسخ آزمایشی" : "Scripted answer";
    const result = { agentRunId: randomUUID(), conversationId: randomUUID(), result: { messages: [{ content: answer }] } };
    if (route === "buffered") return Response.json(result, rotateSession
      ? { headers: { "set-cookie": "scripted-session=renewed; Path=/; HttpOnly; Secure" } }
      : undefined);

    const frames = [{ kind: kind.Started }, { kind: kind.Progress, delta: { stage: 0 } }];
    if (help) frames.push({ kind: kind.Delta, delta: { messageId: result.agentRunId, text: answer } });
    if (canceled) frames.push({ kind: kind.Canceled });
    else if (!truncate) frames.push({ kind: kind.Completed, delta: { result } });
    const wire = frames.map((frame, sequence) => JSON.stringify({
      ...frame, metadata: { sequence: invalidSequence ? sequence + 1 : sequence, operationId: result.agentRunId, timestamp: "scripted" },
    })).join("\n");
    const bytes = new TextEncoder().encode(`${wire}\n`);
    let offset = 0;
    return new Response(new ReadableStream({
      pull(controller) {
        // Small chunks exercise line reassembly and UTF-8 splits without a browser or network.
        if (offset === bytes.length) controller.close();
        else controller.enqueue(bytes.slice(offset, offset += Math.min(7, bytes.length - offset)));
      },
    }), { headers: { "content-type": op.mediaType } });
  };
  return { calls, send };
}

const normal = scriptedMain();
const report = await benchmark(options, normal.send);
assert.equal(report.stop, "finished");
assert.equal(report.samples.length, 34);
const renewed = scriptedMain({ rotateSession: true });
assert.equal((await benchmark(options, renewed.send)).stop, "finished");
const resumed = scriptedMain();
const partial = await benchmark({
  ...options,
  capMicrousd: options.pairCeilingMicrousd,
  previous: { samples: [report.samples[0]] },
}, resumed.send);
assert.equal(partial.stop, "budget");
assert.equal(partial.samples.length, 2);
assert.deepEqual(resumed.calls.map(({ route }) => route), ["antiforgery", "capabilities", "streamed"]);
assert.equal(report.reservedMicrousd, options.capMicrousd);
assert.equal(report.actualSpendMicrousd, null);
assert.ok(report.samples.every(({ outcome }) => outcome === "completed"));
const firstCohort = report.samples.filter(({ scenario, locale }) => scenario === "appointmentCount" && locale === "en");
assert.deepEqual(firstCohort.map(({ transport }) => transport), ["buffered", "streamed", "streamed", "buffered", "shortcut"]);
const submissions = normal.calls.filter(({ message }) => message);
for (let index = 0; index < submissions.length; index += 2) {
  assert.equal(submissions[index].locale, submissions[index + 1].locale);
  assert.equal(submissions[index].message, submissions[index + 1].message);
}
for (const sample of report.samples.filter(({ transport }) => transport === "streamed")) {
  assert.ok(sample.firstAcceptedMs <= sample.firstProgressMs && sample.firstProgressMs <= sample.terminalMs);
  if (sample.scenario === "help") assert.ok(Number.isFinite(sample.firstTextMs) && sample.firstTextMs <= sample.terminalMs);
  else assert.equal(sample.firstTextMs, null);
}
const serialized = JSON.stringify(report);
const bufferedHelp = report.cohorts["help/fa/buffered"];
assert.equal(bufferedHelp.firstTextMs.count, 2);
assert.deepEqual(bufferedHelp.firstTextMs, bufferedHelp.terminalMs);
for (const prohibited of [requestToken, options.sessionCookie, "پاسخ آزمایشی", "Scripted answer", ...submissions.map(({ message }) => message)]) {
  assert.ok(!serialized.includes(prohibited));
}

const interruptedRuns = [];
for (const fault of [{ truncate: true }, { invalidSequence: true }]) {
  const interrupted = scriptedMain(fault);
  const outcome = await benchmark(options, interrupted.send);
  assert.equal(outcome.stop, "uncertain-outcome");
  assert.deepEqual(outcome.samples.map(({ outcome }) => outcome), ["completed", "unknown"]);
  assert.deepEqual(interrupted.calls.map(({ route }) => route), ["antiforgery", "capabilities", "buffered", "streamed"]);
  assert.equal(outcome.samples[1].terminalMs, null);
  assert.deepEqual(outcome.cohorts["appointmentCount/en/streamed"].outcomes, { unknown: 1 });
  interruptedRuns.push({ fault: Object.keys(fault)[0], stop: outcome.stop, samples: outcome.samples });
}

const limited = scriptedMain();
const budget = await benchmark({ ...options, capMicrousd: 15 }, limited.send);
assert.equal(budget.stop, "budget");
assert.equal(budget.samples.length, 2);
assert.equal(budget.reservedMicrousd, 10);
assert.deepEqual(limited.calls.map(({ route }) => route), ["antiforgery", "capabilities", "buffered", "streamed"]);

const canceled = scriptedMain({ canceled: true });
const canceledRun = await benchmark({ ...options, capMicrousd: 10 }, canceled.send);
const canceledSample = canceledRun.samples[1];
assert.equal(canceledSample.outcome, "canceled");
assert.ok(Number.isFinite(canceledSample.terminalMs));
assert.deepEqual(canceledRun.cohorts["appointmentCount/en/streamed"].terminalMs, { count: 0, p50: null, p95: null });

const disabled = scriptedMain({ enabled: false });
await assert.rejects(benchmark(options, disabled.send), /advertise streaming/);
assert.deepEqual(disabled.calls.map(({ route }) => route), ["antiforgery", "capabilities"]);

const incorrect = scriptedMain();
const verified = await benchmark({
  ...options,
  fixture: { ...fixture, expectation: { countTerms: { en: ["known expected answer"] } } },
}, incorrect.send);
assert.equal(verified.stop, "incorrect-result");
assert.equal(verified.samples[0].correct, false);
assert.deepEqual(incorrect.calls.map(({ route }) => route), ["antiforgery", "capabilities", "buffered"]);

const imperfectBaseline = scriptedMain();
const baseline = await benchmark({
  ...options,
  comparisonPhase: "baseline",
  capMicrousd: options.pairCeilingMicrousd,
  fixture: { ...fixture, expectation: { countTerms: { en: ["known expected answer"] } } },
}, imperfectBaseline.send);
assert.equal(baseline.stop, "budget");
assert.deepEqual(baseline.samples.map(({ correct }) => correct), [false, false]);
assert.deepEqual(imperfectBaseline.calls.map(({ route }) => route), ["antiforgery", "capabilities", "buffered", "streamed"]);

process.stdout.write(`${JSON.stringify({
  mode: "scripted",
  revision: execFileSync("git", ["rev-parse", "HEAD"], { encoding: "utf8" }).trim(),
  ...report,
  actualSpendMicrousd: 0,
  interruptedRuns,
  canceledTurn: canceledSample,
  budgetStop: { stop: budget.stop, reservedMicrousd: budget.reservedMicrousd, samples: budget.samples.length },
  disabledCapabilitySubmissions: 0,
}, null, 2)}\n`);

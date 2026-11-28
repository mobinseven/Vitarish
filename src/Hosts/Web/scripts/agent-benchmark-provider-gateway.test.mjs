import assert from "node:assert/strict";
import { once } from "node:events";
import { mkdtemp, rm } from "node:fs/promises";
import { tmpdir } from "node:os";
import { join } from "node:path";
import test from "node:test";
import { benchmarkModel, createGateway, openLedger } from "./agent-benchmark-provider-gateway.mjs";

test("budget gateway preserves reservations, relays SSE incrementally, and refuses traffic beyond the cap", async () => {
  const directory = await mkdtemp(join(tmpdir(), "darman-provider-budget-"));
  let ledger = openLedger(directory, 155);
  let server;
  try {
    assert.throws(() => openLedger(directory));
    const result = { model: benchmarkModel, usage: { input_tokens: 100, output_tokens: 20, total_tokens: 120 } };
    let sent = 0;
    let complete;
    server = createGateway({
      ledger, apiKey: "scripted",
      send: async (url, options) => {
        assert.equal(options.redirect, "error");
        const payload = JSON.parse(options.body);
        if (url.pathname === "/v1/responses/input_tokens") {
          assert.equal(payload.input, "synthetic");
          return Response.json({ input_tokens: 100 });
        }
        assert.equal(url.href, "https://api.openai.com/v1/responses");
        assert.equal(payload.service_tier, "default");
        sent++;
        if (sent === 1) return Response.json(result);
        if (sent === 3) return new Response(null, { status: 502 });
        const encoder = new TextEncoder();
        return new Response(new ReadableStream({
          start(controller) {
            controller.enqueue(encoder.encode('data: {"type":"response.output_text.delta","delta":"آزمایشی"}\n\n'));
            complete = () => {
              controller.enqueue(encoder.encode(`data: ${JSON.stringify({ type: "response.completed", response: result })}\n\n`));
              controller.close();
            };
          },
        }), { headers: { "content-type": "text/event-stream" } });
      },
    });
    server.listen(0, "127.0.0.1");
    await once(server, "listening");
    const origin = `http://127.0.0.1:${server.address().port}`;
    const submit = (overrides = {}) => fetch(`${origin}/v1/responses`, {
      method: "POST",
      body: JSON.stringify({ model: benchmarkModel, max_output_tokens: 100, input: "synthetic", ...overrides }),
    });
    const denied = await submit({ tools: [{ type: "web_search" }] });
    assert.equal(denied.status, 400);
    assert.equal(sent, 0);
    await denied.body.cancel();

    assert.deepEqual(await (await submit()).json(), result);
    assert.equal(ledger.snapshot().chargedUpperBoundMicrousd, 35);

    const streamed = await submit({ stream: true, max_output_tokens: 140 });
    const reader = streamed.body.getReader();
    assert.match(new TextDecoder().decode((await reader.read()).value), /آزمایشی/);
    assert.equal(ledger.snapshot().unresolvedMicrousd, 95);
    const concurrent = await submit();
    assert.equal(concurrent.status, 402);
    await concurrent.body.cancel();
    complete();
    while (!(await reader.read()).done) {}
    assert.equal(ledger.snapshot().chargedUpperBoundMicrousd, 70);
    assert.equal(ledger.snapshot().unresolvedMicrousd, 0);

    const failure = await submit();
    assert.equal(failure.status, 502);
    await failure.body.cancel();
    const beforeRestart = ledger.snapshot();
    assert.equal(beforeRestart.unresolvedMicrousd, 75);
    const blocked = await submit();
    assert.equal(blocked.status, 402);
    await blocked.body.cancel();
    assert.equal(sent, 3);
    await new Promise((resolve) => server.close(resolve));
    server = null;
    ledger.close();
    ledger = undefined;
    ledger = openLedger(directory, 155);
    assert.deepEqual(ledger.snapshot(), beforeRestart);
  } finally {
    if (server) await new Promise((resolve) => server.close(resolve));
    ledger?.close();
    await rm(directory, { recursive: true });
  }
});

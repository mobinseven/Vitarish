import assert from "node:assert/strict";
import http from "node:http";
import test from "node:test";
import { createTwoHostWebRouter } from "./two-host-web-router.mjs";

async function listen(server) {
  await new Promise(resolve => server.listen(0, "127.0.0.1", resolve));
  return `http://127.0.0.1:${server.address().port}`;
}
async function close(...servers) {
  for (const server of servers.reverse()) {
    server.closeAllConnections();
    await new Promise(resolve => server.close(resolve));
  }
}
function fixtureRequest(url, { body, ...options }) {
  return new Promise((resolve, reject) => {
    const request = http.request(url, options, response => {
      let text = "";
      response.on("data", chunk => { text += chunk; });
      response.on("end", () => resolve({ headers: response.headers, text }));
    });
    request.on("error", reject);
    request.end(body);
  });
}

test("test ingress selects the actual app without rewriting host, query, body or host-only cookies", async () => {
  const owner = http.createServer((request, response) => response.end("owner"));
  const landing = http.createServer(async (request, response) => {
    let body = "";
    for await (const chunk of request) body += chunk;
    response.setHeader("Set-Cookie", "fixture_session=fixture; HttpOnly; Path=/");
    response.end(JSON.stringify({ host: request.headers.host, url: request.url, cookie: request.headers.cookie, origin: request.headers.origin, body }));
  });
  const ownerTarget = await listen(owner);
  const landingTarget = await listen(landing);
  const landingHost = "landing.example.test";
  const router = createTwoHostWebRouter({ landingHost, landingTarget, ownerTarget });
  const origin = await listen(router);
  try {
    const response = await fixtureRequest(origin + "/entry?keep=a%20b~c&k=1", { method: "POST", headers: { Host: landingHost, Cookie: "fixture_session=fixture", Origin: "http://landing.example.test" }, body: "fixture-body" });
    assert.deepEqual(JSON.parse(response.text), { host: landingHost, url: "/entry?keep=a%20b~c&k=1", cookie: "fixture_session=fixture", origin: "http://landing.example.test", body: "fixture-body" });
    assert.deepEqual(response.headers["set-cookie"], ["fixture_session=fixture; HttpOnly; Path=/"]);
    assert.equal(await (await fetch(origin, { headers: { Host: "clinic.example.test" } })).text(), "owner");
  } finally { await close(owner, landing, router); }
});

test("test ingress streams before completion and cancels the upstream when the browser leaves", async () => {
  let observeClose;
  const closed = new Promise(resolve => { observeClose = resolve; });
  const app = http.createServer((request, response) => {
    response.setHeader("Content-Type", "text/event-stream");
    response.write("data: fixture\n\n");
    response.once("close", observeClose);
  });
  const target = await listen(app);
  const router = createTwoHostWebRouter({ landingHost: "landing.example.test", landingTarget: target, ownerTarget: target });
  const origin = await listen(router);
  const controller = new AbortController();
  try {
    const response = await fetch(origin, { signal: controller.signal });
    assert.equal(new TextDecoder().decode((await response.body.getReader().read()).value), "data: fixture\n\n");
    controller.abort();
    await Promise.race([closed, new Promise((_, reject) => { const timer = setTimeout(() => reject(new Error("Upstream stream was not cancelled")), 1000); timer.unref(); })]);
  } finally { await close(app, router); }
});

test("an unavailable upstream becomes a dependency failure rather than successful empty content", async () => {
  const app = http.createServer(request => request.socket.destroy());
  const target = await listen(app);
  const router = createTwoHostWebRouter({ landingHost: "landing.example.test", landingTarget: target, ownerTarget: target });
  const origin = await listen(router);
  try { assert.equal((await fetch(origin)).status, 502); }
  finally { await close(app, router); }
});

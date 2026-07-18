#!/usr/bin/env node

import { createServer } from "node:http";

const host = "127.0.0.1";
const port = Number.parseInt(process.env.VITARISH_LAYOUT_FIXTURE_PORT ?? "4100", 10);
const allowedOrigins = new Set([
  "http://127.0.0.1:3000",
  "http://127.0.0.1:3001",
]);

const workspace = {
  corpora: [
    {
      corpusId: "clinical-guidance",
      name: "Clinical guidance",
      description: "Deterministic fixture corpus",
      isActive: true,
      sourceCount: 3,
      chunkCount: 12,
    },
    {
      corpusId: "practice-operations",
      name: "Practice operations",
      description: "Deterministic fixture corpus",
      isActive: true,
      sourceCount: 2,
      chunkCount: 8,
    },
  ],
};

const server = createServer(async (request, response) => {
  applyCors(request, response);

  if (request.method === "OPTIONS") {
    response.writeHead(204);
    response.end();
    return;
  }

  const url = new URL(request.url ?? "/", `http://${host}:${port}`);

  if (request.method === "GET" && url.pathname === "/health") {
    sendJson(response, 200, { status: "ready" });
    return;
  }

  if (
    request.method === "GET" &&
    url.pathname === "/api/modules/authentication/session"
  ) {
    const isAuthenticated = parseCookies(request.headers.cookie)["vitarish.layout-auth"] === "authenticated";
    sendJson(response, 200, createSession(isAuthenticated));
    return;
  }

  if (
    request.method === "GET" &&
    url.pathname === "/api/modules/authentication/antiforgery"
  ) {
    sendJson(response, 200, {
      requestToken: "layout-fixture-antiforgery-token",
      headerName: "X-XSRF-TOKEN",
    });
    return;
  }

  if (request.method === "GET" && url.pathname === "/api/modules/journey/current") {
    sendJson(response, 200, {
      step: {
        destinationUrl: "/home",
        destinationKind: "UserHome",
        nonBlockingGates: [],
      },
    });
    return;
  }

  if (request.method === "GET" && url.pathname === "/api/modules/knowledge/workspace") {
    sendJson(response, 200, workspace);
    return;
  }

  if (request.method === "POST" && url.pathname === "/api/modules/knowledge/ask") {
    await readRequestBody(request);
    sendJson(response, 200, {
      answer:
        "The deterministic fixture answer keeps browser geometry stable. " +
        "It contains enough grounded detail to exercise the workspace conversation scroller " +
        "without contacting any live service or tenant data.",
      isAnswered: true,
      citations: [
        {
          chunkId: "fixture-chunk-1",
          sourceId: "fixture-source-1",
          documentId: "fixture-document-1",
          title: "Fixture source",
          quote: "Deterministic layout evidence.",
        },
      ],
      retrievedContext: [
        {
          chunkId: "fixture-chunk-1",
          sourceId: "fixture-source-1",
          documentId: "fixture-document-1",
          text: "Deterministic layout evidence for responsive browser validation.",
          title: "Fixture source",
          score: 0.94,
        },
      ],
    });
    return;
  }

  sendJson(response, 404, {
    status: 404,
    title: "Fixture endpoint not found",
    detail: `${request.method ?? "GET"} ${url.pathname}`,
  });
});

server.listen(port, host, () => {
  console.log(`Layout fixture service listening on http://${host}:${port}`);
});

function applyCors(request, response) {
  const origin = request.headers.origin;
  if (origin && allowedOrigins.has(origin)) {
    response.setHeader("Access-Control-Allow-Origin", origin);
    response.setHeader("Access-Control-Allow-Credentials", "true");
    response.setHeader("Vary", "Origin");
  }
  response.setHeader("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
  response.setHeader(
    "Access-Control-Allow-Headers",
    "Accept, Content-Type, X-Vitarish-Tenant-Host, X-XSRF-TOKEN"
  );
}

function createSession(isAuthenticated) {
  return {
    isAuthenticated,
    user: isAuthenticated
      ? {
          userId: "layout-fixture-user",
          displayName: "Layout Fixture",
          givenName: "Layout",
          familyName: "Fixture",
          email: "layout.fixture@example.test",
          phoneNumber: null,
        }
      : null,
    tenantAccess: {
      canAccessTenantOwner: isAuthenticated,
      canAccessTenantUser: isAuthenticated,
    },
  };
}

function parseCookies(header = "") {
  return Object.fromEntries(
    header
      .split(";")
      .map((part) => part.trim())
      .filter(Boolean)
      .map((part) => {
        const separator = part.indexOf("=");
        return separator === -1
          ? [part, ""]
          : [part.slice(0, separator), decodeURIComponent(part.slice(separator + 1))];
      })
  );
}

function sendJson(response, status, body) {
  response.writeHead(status, { "Content-Type": "application/json; charset=utf-8" });
  response.end(JSON.stringify(body));
}

async function readRequestBody(request) {
  for await (const _chunk of request) {
    // Drain the request so keep-alive connections remain reusable.
  }
}

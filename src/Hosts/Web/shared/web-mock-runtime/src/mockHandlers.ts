import { HttpResponse, http, type RequestHandler } from "msw";
import { createMockStore } from "./mockStore";

export type MockScenario = "ready" | "empty" | "loading" | "error";
export type MockHandlerFactory = (scenario: MockScenario) => readonly RequestHandler[];

const scenarios = new Set<MockScenario>(["ready", "empty", "loading", "error"]);

export function resolveMockScenario(value: string | undefined): MockScenario {
  return scenarios.has(value as MockScenario) ? (value as MockScenario) : "ready";
}

export function createDefaultMockHandlers(activeScenario: MockScenario): RequestHandler[] {
  const delay = activeScenario === "loading" ? 750 : 0;
  const withDelay = async () => {
    if (delay > 0) await new Promise((resolve) => setTimeout(resolve, delay));
  };
  const unavailable = () => HttpResponse.json(
    { status: 503, code: "MockUnavailable", title: "Mock service unavailable" },
    { status: 503 },
  );
  const store = createMockStore(activeScenario === "empty" ? "empty" : "ready");
  const badRequest = (detail: string) => HttpResponse.json({ status: 400, code: "InvalidRequest", title: "Invalid mock request", detail }, { status: 400 });
  const notFound = (detail: string) => HttpResponse.json({ status: 404, code: "NotFound", title: "Mock entity not found", detail }, { status: 404 });

  return [
    http.get(/\/api\/modules\/authentication\/antiforgery(?:\?|$)/, async () => {
      await withDelay();
      return HttpResponse.json({ requestToken: "mock-antiforgery-token", headerName: "X-XSRF-TOKEN" });
    }),
    http.get(/\/api\/modules\/authentication\/session(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return HttpResponse.json({
        isAuthenticated: true,
        user: {
          userId: "mock-user",
          displayName: "Mock User",
          givenName: "Mock",
          familyName: "User",
          email: "mock.user@example.test",
          phoneNumber: null,
        },
        canAccessPlatformAdmin: true,
        tenantAccess: { canAccessTenantOwner: true, canAccessTenantUser: true },
      });
    }),
    http.get(/\/api\/modules\/journey\/current(?:\?|$)/, async () => {
      await withDelay();
      return HttpResponse.json({ step: { destinationUrl: "/home", destinationKind: "UserHome", nonBlockingGates: [] } });
    }),
    http.get(/\/api\/modules\/knowledge\/workspace(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return HttpResponse.json({
        corpora: activeScenario === "empty" ? [] : [{
          corpusId: "mock-clinical-guidance",
          name: "Clinical guidance",
          description: "Deterministic mock corpus for interface review.",
          isActive: true,
          sourceCount: 3,
          chunkCount: 12,
        }],
      });
    }),
    http.post(/\/api\/modules\/knowledge\/ask(?:\?|$)/, async ({ request }) => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      const body = await request.json().catch(() => null) as { question?: unknown; query?: unknown } | null;
      const question = typeof body?.question === "string" ? body.question : typeof body?.query === "string" ? body.query : "your question";
      store.knowledgeAsks.push(question);
      return HttpResponse.json({
        answer: `Deterministic mock answer ${store.knowledgeAsks.length}: this is grounded guidance for “${question}”.`,
        isAnswered: true,
        citations: [{ chunkId: "mock-chunk-1", sourceId: "mock-source-1", documentId: "mock-document-1", title: "Mock guidance", quote: "Deterministic mock evidence." }],
        retrievedContext: [{ chunkId: "mock-chunk-1", sourceId: "mock-source-1", documentId: "mock-document-1", title: "Mock guidance", text: "Deterministic mock evidence for interface review.", score: 0.94 }],
      });
    }),
    http.post(/\/api\/modules\/clinic\/patients(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return HttpResponse.json(store.patients.filter((patient) => !patient.isDeleted));
    }),
    http.post(/\/api\/modules\/clinic\/patients\/count(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return HttpResponse.json(store.patients.filter((patient) => !patient.isDeleted).length);
    }),
    http.get(/\/api\/modules\/clinic\/patients\/[^/]+(?:\?|$)/, async ({ request }) => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      const id = new URL(request.url).pathname.split("/").at(-1);
      const patient = store.patients.find((item) => item.id === id && !item.isDeleted);
      return patient ? HttpResponse.json(patient) : notFound("Patient does not exist in the mock store.");
    }),
    http.post(/\/api\/modules\/clinic\/patients\/find-or-create(?:\?|$)/, async ({ request }) => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      const body = await request.json().catch(() => null) as Record<string, unknown> | null;
      const firstName = typeof body?.firstName === "string" ? body.firstName.trim() : "";
      const lastName = typeof body?.lastName === "string" ? body.lastName.trim() : "";
      if (!firstName || !lastName) return badRequest("firstName and lastName are required");
      const patient = { id: store.nextId("mock-patient"), tenantId: "mock-tenant", name: `${firstName} ${lastName}`, firstName, lastName, identificationCode: `mock-${store.nextId("code")}`, createdAt: "2026-01-02T00:00:00.000Z", isDeleted: false };
      store.patients.push(patient);
      return HttpResponse.json(patient, { status: 201 });
    }),
    http.put(/\/api\/modules\/clinic\/patients\/[^/]+(?:\?|$)/, async ({ request, params }) => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      const patient = store.patients.find((item) => item.id === String(params[0]));
      if (!patient) return notFound("Patient does not exist in the mock store.");
      const body = await request.json().catch(() => null) as Record<string, unknown> | null;
      if (typeof body?.firstName === "string") patient.firstName = body.firstName;
      if (typeof body?.lastName === "string") patient.lastName = body.lastName;
      patient.name = `${patient.firstName} ${patient.lastName}`;
      return HttpResponse.json(patient);
    }),
    http.get(/\/api\/modules\/parties\/administration\/owners(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return HttpResponse.json(store.owners);
    }),
    http.get(/\/api\/modules\/parties\/administration\/tenants(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return HttpResponse.json(store.tenants);
    }),
    http.get(/\/api\/modules\/parties\/administration\/owners\/[^/]+(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      const owner = store.owners.find((item) => item.id === "mock-owner");
      return owner ? HttpResponse.json(owner) : notFound("Owner does not exist in the mock store.");
    }),
    http.get(/\/api\/modules\/parties\/administration\/tenants\/[^/]+(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      const tenant = store.tenants.find((item) => item.id === "mock-tenant");
      return tenant ? HttpResponse.json(tenant) : notFound("Tenant does not exist in the mock store.");
    }),
    http.get(/\/api\/modules\/parties\/administration\/owners\/[^/]+\/tenants(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return HttpResponse.json(store.tenants);
    }),
    http.get(/\/api\/modules\/parties\/administration\/tenants\/[^/]+\/indicators(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return HttpResponse.json(store.indicators);
    }),
    http.get(/\/api\/modules\/users\/administration\/[^/]+\/tenant-roles(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return HttpResponse.json(store.memberships.filter((membership) => membership.userId === "mock-user").map(({ userId: _userId, ...membership }) => membership));
    }),
    http.get(/\/api\/modules\/users\/search(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return HttpResponse.json(store.users);
    }),
    http.all(/\/api\/modules\/(?:parties\/administration|users\/administration)\/.+/, async ({ request }) => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      if (request.method === "GET") return HttpResponse.json([]);
      const body = await request.json().catch(() => null) as Record<string, unknown> | null;
      const path = new URL(request.url).pathname;
      if (path.endsWith("/owners") && request.method === "POST") {
        const owner = { id: typeof body?.id === "string" ? body.id : store.nextId("mock-owner"), name: typeof body?.name === "string" ? body.name : "Mock Owner", description: typeof body?.description === "string" ? body.description : "" };
        store.owners.push(owner);
        return HttpResponse.json(owner, { status: 201 });
      }
      if (path.endsWith("/tenants") && request.method === "POST") {
        const tenant = { id: typeof body?.id === "string" ? body.id : store.nextId("mock-tenant"), name: typeof body?.name === "string" ? body.name : "Mock Tenant", appId: typeof body?.appId === "string" ? body.appId : "private-practice-owner", ownerId: typeof body?.ownerId === "string" ? body.ownerId : "mock-owner" };
        store.tenants.push(tenant);
        return HttpResponse.json(tenant, { status: 201 });
      }
      if (path.includes("/tenant-roles") && request.method === "POST") {
        const membership = { userId: path.split("/").at(-2) ?? "mock-user", tenantId: typeof body?.tenantId === "string" ? body.tenantId : "mock-tenant", roleSuffix: typeof body?.roleSuffix === "string" ? body.roleSuffix : "TenantUser" };
        store.memberships.push(membership);
        return HttpResponse.json(membership, { status: 201 });
      }
      return HttpResponse.json({ succeeded: true, id: typeof body?.id === "string" ? body.id : "mock-entity" });
    }),
    http.post(/\/api\/apps\/landing\/conversations(?:\?|$)/, async ({ request }) => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      let message = "";
      let body: { message?: unknown; ownerKey?: unknown; conversationId?: unknown };
      try {
        body = await request.json() as { message?: unknown; ownerKey?: unknown; conversationId?: unknown };
        message = typeof body.message === "string" ? body.message.trim() : "";
      } catch {
        return HttpResponse.json({ status: 400, code: "InvalidRequest", title: "Invalid mock request" }, { status: 400 });
      }
      const ownerKey = typeof body?.ownerKey === "string" ? body.ownerKey : "home";
      const requestedId = typeof body?.conversationId === "string" ? body.conversationId : null;
      const existing = requestedId ? store.conversations.find((item) => item.id === requestedId) : undefined;
      const conversation = existing ?? (() => {
        const created = { id: store.nextId("mock-conversation"), ownerKey, messages: [] as Array<{ role: "user" | "assistant"; text: string }> };
        store.conversations.push(created);
        return created;
      })();
      const reply = message ? `Mock guidance for: ${message}` : "This deterministic mock reply is ready for interface review.";
      conversation.messages.push({ role: "user", text: message }, { role: "assistant", text: reply });
      return HttpResponse.json({ conversationId: conversation.id, message: reply });
    }),
    http.delete(/\/api\/apps\/landing\/conversations\/[^/]+(?:\?|$)/, async ({ request }) => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      const id = new URL(request.url).pathname.split("/").at(-1);
      const index = store.conversations.findIndex((item) => item.id === id);
      if (index < 0) return notFound("Conversation does not exist in the mock store.");
      store.conversations.splice(index, 1);
      return new HttpResponse(null, { status: 204 });
    }),
    http.post(/\/api\/modules\/appointments\/(reservations|shifts|service-types|services|service-providers)(?:\?|$)/, async ({ request }) => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      const path = new URL(request.url).pathname;
      const resource = Object.keys(store.appointments).find((key) => path.endsWith(`/${key}`)) as keyof typeof store.appointments | undefined;
      return HttpResponse.json(resource ? store.appointments[resource] : []);
    }),
    http.all(/\/api\/modules\/appointments\/.*(?:\/create|\/status)?(?:\?|$)/, async ({ request }) => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      if (request.method === "POST" || request.method === "PUT" || request.method === "PATCH") {
        const path = new URL(request.url).pathname;
        const resource = Object.keys(store.appointments).find((key) => path.includes(`/appointments/${key}`)) as keyof typeof store.appointments | undefined;
        const body = await request.json().catch(() => null) as Record<string, unknown> | null;
        if (resource && path.endsWith("/create")) {
          const created = { ...(body ?? {}), id: typeof body?.id === "string" ? body.id : store.nextId("mock-appointment"), createdAt: "2026-01-02T00:00:00.000Z" };
          store.appointments[resource].push(created);
          return HttpResponse.json(created, { status: 201 });
        }
      }
      return HttpResponse.json({ succeeded: true, entityId: "mock-appointment-entity" });
    }),
    http.all(/\/api\/modules\/sea\/.+/, async ({ request }) => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      if (request.method !== "GET") {
        const body = await request.json().catch(() => null) as Record<string, unknown> | null;
        return HttpResponse.json({ ...(body ?? {}), id: typeof body?.id === "string" ? body.id : "mock-sea-entity" }, { status: 201 });
      }
      return HttpResponse.json([]);
    }),
    http.get(/\/api\/modules\/intelligence\/transcription\/live\/capabilities(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return HttpResponse.json({
        available: false,
        languages: ["en-US", "fa-IR"],
        mimeTypes: ["audio/webm"],
        limits: { maxDurationSeconds: 300, maxTotalBytes: 10_000_000, maxFrameBytes: 256_000, idleTimeoutSeconds: 30 },
      });
    }),
    http.post(/\/api\/modules\/intelligence\/(responder|transcription|rewrite)(?:\/stream)?(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return HttpResponse.json({
        text: "Deterministic mock intelligence result for interface review.",
        answer: "Deterministic mock intelligence result for interface review.",
        isAnswered: true,
        citations: [],
      });
    }),
    http.post(/\/api\/modules\/intelligence\/agent-conversations(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return HttpResponse.json({
        agentRunId: "mock-agent-run",
        conversationId: "mock-agent-conversation",
        result: { text: "Deterministic mock agent result." },
      });
    }),
  ];
}

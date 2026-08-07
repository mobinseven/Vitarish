import { HttpResponse, http, type RequestHandler } from "msw";

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
    http.post(/\/api\/modules\/knowledge\/ask(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return HttpResponse.json({
        answer: "This deterministic mock answer is for reviewing the knowledge interface without a live provider.",
        isAnswered: true,
        citations: [{ chunkId: "mock-chunk-1", sourceId: "mock-source-1", documentId: "mock-document-1", title: "Mock guidance", quote: "Deterministic mock evidence." }],
        retrievedContext: [{ chunkId: "mock-chunk-1", sourceId: "mock-source-1", documentId: "mock-document-1", title: "Mock guidance", text: "Deterministic mock evidence for interface review.", score: 0.94 }],
      });
    }),
    http.post(/\/api\/modules\/clinic\/patients(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return HttpResponse.json(activeScenario === "empty" ? [] : [
        {
          id: "mock-patient-ada",
          tenantId: "mock-tenant",
          name: "Ada Lovelace",
          firstName: "Ada",
          lastName: "Lovelace",
          identificationCode: "mock-ada-001",
          phone: "+10000000001",
          email: "ada@example.test",
          dateOfBirth: "1990-12-10",
          createdAt: "2026-01-01T00:00:00.000Z",
          isDeleted: false,
        },
      ]);
    }),
    http.post(/\/api\/modules\/clinic\/patients\/count(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return HttpResponse.json(activeScenario === "empty" ? 0 : 1);
    }),
    http.get(/\/api\/modules\/parties\/administration\/owners(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return HttpResponse.json(activeScenario === "empty" ? [] : [{ id: "mock-owner", name: "Mock Practice", description: "Deterministic mock owner." }]);
    }),
    http.get(/\/api\/modules\/parties\/administration\/tenants(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return HttpResponse.json(activeScenario === "empty" ? [] : [{ id: "mock-tenant", name: "Mock Tenant", appId: "private-practice-owner", ownerId: "mock-owner" }]);
    }),
    http.get(/\/api\/modules\/parties\/administration\/owners\/[^/]+(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return HttpResponse.json({ id: "mock-owner", name: "Mock Practice", description: "Deterministic mock owner." });
    }),
    http.get(/\/api\/modules\/parties\/administration\/tenants\/[^/]+(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return HttpResponse.json({ id: "mock-tenant", name: "Mock Tenant", appId: "private-practice-owner", ownerId: "mock-owner" });
    }),
    http.get(/\/api\/modules\/parties\/administration\/owners\/[^/]+\/tenants(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return HttpResponse.json(activeScenario === "empty" ? [] : [{ id: "mock-tenant", name: "Mock Tenant", appId: "private-practice-owner", ownerId: "mock-owner" }]);
    }),
    http.get(/\/api\/modules\/parties\/administration\/tenants\/[^/]+\/indicators(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return HttpResponse.json(activeScenario === "empty" ? [] : [{ id: "mock-indicator", tenantId: "mock-tenant", value: "mock.localhost", method: 1, status: 3, isPrimary: true }]);
    }),
    http.get(/\/api\/modules\/users\/administration\/[^/]+\/tenant-roles(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return HttpResponse.json(activeScenario === "empty" ? [] : [{ tenantId: "mock-tenant", roleSuffix: "Owner" }]);
    }),
    http.get(/\/api\/modules\/users\/search(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return HttpResponse.json(activeScenario === "empty" ? [] : [{ userId: "mock-user", fullName: "Mock User", userName: "mock.user" }]);
    }),
    http.all(/\/api\/modules\/(?:parties\/administration|users\/administration)\/.+/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return HttpResponse.json({ succeeded: true, id: "mock-entity" });
    }),
    http.post(/\/api\/apps\/landing\/conversations(?:\?|$)/, async ({ request }) => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      let message = "";
      try {
        const body = await request.json() as { message?: unknown };
        message = typeof body.message === "string" ? body.message.trim() : "";
      } catch {
        return HttpResponse.json({ status: 400, code: "InvalidRequest", title: "Invalid mock request" }, { status: 400 });
      }
      return HttpResponse.json({
        conversationId: "mock-landing-conversation",
        message: message
          ? `Mock guidance for: ${message}`
          : "This deterministic mock reply is ready for interface review.",
      });
    }),
    http.delete(/\/api\/apps\/landing\/conversations\/[^/]+(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return new HttpResponse(null, { status: 204 });
    }),
    http.post(/\/api\/modules\/appointments\/(reservations|shifts|service-types|services|service-providers)(?:\?|$)/, async ({ request }) => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      const path = new URL(request.url).pathname;
      if (activeScenario === "empty") return HttpResponse.json([]);
      if (path.endsWith("/reservations")) {
        return HttpResponse.json([{ id: "mock-reservation", createdAt: "2026-01-01T00:00:00.000Z", isDeleted: false, status: 1, shiftId: "mock-shift", date: "2026-08-11", isHold: false }]);
      }
      if (path.endsWith("/shifts")) {
        return HttpResponse.json([{ id: "mock-shift", createdAt: "2026-01-01T00:00:00.000Z", isDeleted: false, status: 1, visibility: 1, dayOfWeek: 2, startTime: "10:30", endTime: "11:00", timeZone: "UTC", totalSeats: 1, serviceProviderId: "mock-provider", serviceId: "mock-service" }]);
      }
      if (path.endsWith("/service-types")) {
        return HttpResponse.json([{ id: "mock-service-type", createdAt: "2026-01-01T00:00:00.000Z", isDeleted: false, name: "Consultation", description: "Mock consultation" }]);
      }
      if (path.endsWith("/services")) {
        return HttpResponse.json([{ id: "mock-service", createdAt: "2026-01-01T00:00:00.000Z", isDeleted: false, name: "Initial consultation", description: "Mock service", serviceTypeId: "mock-service-type", serviceTypeName: "Consultation" }]);
      }
      return HttpResponse.json([{ id: "mock-provider", createdAt: "2026-01-01T00:00:00.000Z", isDeleted: false, name: "Mock clinician", description: "Mock provider", linkedUserIds: ["mock-user"] }]);
    }),
    http.all(/\/api\/modules\/appointments\/.*(?:\/create|\/status)?(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return HttpResponse.json({ succeeded: true, entityId: "mock-appointment-entity" });
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

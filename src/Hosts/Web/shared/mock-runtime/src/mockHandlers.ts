import { HttpResponse, http, type RequestHandler } from "msw";
import { parseLocaleCookie } from "@vitarish/localization";
import { createMockStore } from "./mockStore";
import { isMockAnonymousSession } from "./mockSession";
import { createAuthMockHandlers } from "./mockAuthHandlers";
import { createSeaMockHandlers } from "./mockSeaHandlers";
import { createSurgeryMockHandlers } from "./mockSurgeryHandlers";

export type MockScenario = "ready" | "empty" | "loading" | "error";
export type MockHandlerFactory = (scenario: MockScenario) => readonly RequestHandler[];

const scenarios = new Set<MockScenario>(["ready", "empty", "loading", "error"]);

function isPersianRequest(request: Request) {
  return (parseLocaleCookie(request.headers.get("cookie")) ?? "fa") === "fa";
}

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
  let taminCredentialId = "mock-tamin-credential";
  let taminAuthorized = false;
  const badRequest = (detail: string) => HttpResponse.json({ status: 400, code: "InvalidRequest", title: "Invalid mock request", detail }, { status: 400 });
  const notFound = (detail: string) => HttpResponse.json({ status: 404, code: "NotFound", title: "Mock entity not found", detail }, { status: 404 });

  const filteredAuditEvents = (request: Request) => {
    const query = new URL(request.url).searchParams;
    const matches = store.auditEvents.filter((event) => {
      const fromUtc = query.get("fromUtc");
      const toUtc = query.get("toUtc");
      return (!query.get("tenantId") || event.tenantId === query.get("tenantId"))
        && (!query.get("actorId") || event.actorId === query.get("actorId"))
        && (!query.get("eventName") || event.eventName === query.get("eventName"))
        && (!query.get("eventCategory") || event.eventCategory === query.get("eventCategory"))
        && (!query.get("operation") || event.operation === query.get("operation"))
        && (!query.get("outcome") || event.outcome === query.get("outcome"))
        && (!query.get("eventId") || event.id === query.get("eventId"))
        && (!fromUtc || event.occurredAt >= fromUtc)
        && (!toUtc || event.occurredAt <= toUtc)
        && (query.get("uncataloguedOnly") !== "true" || !store.auditCatalog.some((entry) => entry.category === event.eventCategory && entry.eventName === event.eventName && (entry.isWildcard || entry.operation === event.operation)));
    });
    const descending = query.get("descending") !== "false";
    const sortBy = query.get("sortBy") ?? "1";
    const sortValue = (event: (typeof matches)[number]) => {
      switch (sortBy) {
        case "0": return event.actorId;
        case "2": return event.eventName;
        case "3": return event.actorKind;
        case "4": return event.sourceIpKind;
        case "5": return event.sourceIpAddress;
        default: return event.occurredAt;
      }
    };
    matches.sort((left, right) => {
      const leftValue = sortValue(left);
      const rightValue = sortValue(right);
      return (leftValue < rightValue ? -1 : leftValue > rightValue ? 1 : 0) * (descending ? -1 : 1);
    });
    return matches;
  };

  const auditCsv = (events: typeof store.auditEvents) => [
    "occurredAt,tenantId,eventCategory,eventName,operation,outcome,actorKind,actorId,sourceIpAddress",
    ...events.map((event) => [event.occurredAt, event.tenantId ?? "", event.eventCategory, event.eventName, event.operation, event.outcome, event.actorKind, event.actorId, event.sourceIpAddress].map((value) => `"${value.replaceAll('"', '""')}"`).join(",")),
  ].join("\n");

  return [
    http.get(/\/api\/modules\/authentication\/antiforgery(?:\?|$)/, async () => {
      await withDelay();
      return HttpResponse.json({ requestToken: "mock-antiforgery-token", headerName: "X-XSRF-TOKEN" });
    }),
    http.get(/\/api\/modules\/authentication\/session(?:\?|$)/, async ({ request }) => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      if (isMockAnonymousSession(request.headers.get("cookie"))) {
        return HttpResponse.json({
          isAuthenticated: false,
          user: null,
          canAccessPlatformAdmin: false,
          tenantAccess: { canAccessTenantOwner: false, canAccessTenantUser: false },
        });
      }
      const isPersian = isPersianRequest(request);
      return HttpResponse.json({
        isAuthenticated: true,
        user: {
          userId: "mock-user",
          displayName: isPersian ? "کاربر نمونه" : "Mock User",
          givenName: isPersian ? "کاربر" : "Mock",
          familyName: isPersian ? "نمونه" : "User",
          email: "mock.user@example.test",
          phoneNumber: null,
        },
        canAccessPlatformAdmin: true,
        tenantAccess: { canAccessTenantOwner: true, canAccessTenantUser: true },
      });
    }),
    http.get(/\/api\/modules\/authentication\/profile(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return HttpResponse.json({ allowPasswordlessPhoneLogin: true, allowPasswordlessEmailLogin: true, allowPasskeyLogin: true });
    }),
    http.get(/\/api\/modules\/journey\/current(?:\?|$)/, async () => {
      await withDelay();
      return HttpResponse.json({ step: { destinationUrl: "/home", destinationKind: "UserHome", nonBlockingGates: [] } });
    }),
    http.get(/\/api\/audit\/platform\/security-event-catalog(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return HttpResponse.json(activeScenario === "empty" ? [] : store.auditCatalog);
    }),
    http.get(/\/api\/audit\/platform\/security-events\/export(?:\?|$)/, async ({ request }) => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return new HttpResponse(auditCsv(filteredAuditEvents(request)), { headers: { "Content-Type": "text/csv; charset=utf-8", "Content-Disposition": "attachment; filename=security-audit-events.csv" } });
    }),
    // Tenant and platform audit lists share the paged result shape.
    http.get(/\/api\/audit\/(?:platform\/)?security-events(?:\?|$)/, async ({ request }) => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      const query = new URL(request.url).searchParams;
      const page = Math.max(1, Number(query.get("page") ?? "1"));
      const pageSize = Math.max(1, Number(query.get("pageSize") ?? "25"));
      const events = filteredAuditEvents(request);
      return HttpResponse.json({
        items: events.slice((page - 1) * pageSize, page * pageSize),
        page,
        pageSize,
        totalCount: events.length,
        capacityThresholdReached: false,
      });
    }),
    http.get(/\/api\/modules\/parties\/platform\/tenants(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return HttpResponse.json(store.tenants.map((tenant) => ({ ...tenant, metadata: {}, ownerName: "Mock Practice" })));
    }),
    http.get(/\/api\/modules\/clinic\/dashboard\/appointments(?:\?|$)/, async ({ request }) => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      if (activeScenario === "empty") return HttpResponse.json([]);

      const date = new URL(request.url).searchParams.get("dateFrom") ?? "2026-01-01";
      const isPersian = isPersianRequest(request);
      const localizedPatientNames = ["آدا لاولیس", "گریس هاپر", "کاترین جانسون", "ماری کوری"];
      const patientTags = [["VIP"], [], ["Follow-up", "Diabetes"], ["Allergy"]];
      const localizedPatientTags = [["ویژه"], [], ["پیگیری", "دیابت"], ["حساسیت"]];
      const schedule = [
        { patient: store.patients[0], startTime: "09:30:00", status: 1 },
        { patient: store.patients[1], startTime: "11:00:00", status: 0 },
        { patient: store.patients[2], startTime: "14:15:00", status: 7 },
        { patient: store.patients[3], startTime: "16:00:00", status: 5 },
      ];

      return HttpResponse.json(
        schedule.map(({ patient, startTime, status }, index) => ({
          reservationId: `mock-dashboard-reservation-${index + 1}`,
          patientId: patient.id,
          patientDisplayName: isPersian ? localizedPatientNames[index] : patient.name,
          serviceId: "mock-service",
          serviceDisplayName: isPersian
            ? index % 2 === 0 ? "ویزیت اولیه" : "ویزیت پیگیری"
            : index % 2 === 0 ? "Initial consultation" : "Follow-up visit",
          date,
          startTime,
          timeZone: "UTC",
          status,
          patientTags: (isPersian ? localizedPatientTags : patientTags)[index],
        })),
      );
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
    http.put(/\/api\/modules\/clinic\/patients\/[^/]+(?:\?|$)/, async ({ request }) => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      const id = new URL(request.url).pathname.split("/").at(-1);
      const patient = store.patients.find((item) => item.id === id);
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
    http.get(/\/api\/modules\/parties\/administration\/owners\/[^/]+(?:\?|$)/, async ({ request }) => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      const id = new URL(request.url).pathname.split("/").at(-1);
      const owner = store.owners.find((item) => item.id === id);
      return owner ? HttpResponse.json(owner) : notFound("Owner does not exist in the mock store.");
    }),
    http.get(/\/api\/modules\/parties\/administration\/tenants\/[^/]+(?:\?|$)/, async ({ request }) => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      const id = new URL(request.url).pathname.split("/").at(-1);
      const tenant = store.tenants.find((item) => item.id === id);
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
    http.get(/\/api\/modules\/users\/tenant-members(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return HttpResponse.json(store.memberships.flatMap(({ userId, roleSuffix }) => {
        const user = store.users.find((candidate) => candidate.userId === userId);
        return user ? [{
          roleSuffix,
          user: { id: user.userId, userName: user.userName, fullName: user.fullName, firstName: null, lastName: null, email: user.email ?? null, phoneNumber: null, identificationCode: null, dateOfBirth: null, birthCountryCode: null, birthCity: null },
        }] : [];
      }));
    }),
    http.get(/\/api\/modules\/users\/search(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return HttpResponse.json(store.users);
    }),
    http.get(/\/api\/modules\/authentication\/security\/users\/[^/]+\/inactivity-timeout(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return HttpResponse.json({ version: 1, inactivityTimeoutMinutes: null });
    }),
    // A user's security detail pages its security and sign-in histories independently.
    http.get(/\/api\/modules\/users\/administration\/[^/]+\/security(?:\?|$)/, async ({ request }) => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      const url = new URL(request.url);
      const userId = decodeURIComponent(url.pathname.split("/").at(-2) ?? "mock-user");
      const pageSize = Math.max(1, Number(url.searchParams.get("pageSize") ?? "25"));
      const securityPage = Math.max(1, Number(url.searchParams.get("page") ?? "1"));
      const authenticationPage = Math.max(1, Number(url.searchParams.get("authenticationPage") ?? String(securityPage)));
      const history = (count: number, page: number, event: (index: number) => Record<string, unknown>) => ({
        items: Array.from({ length: count }, (_, index) => event(index)).slice((page - 1) * pageSize, page * pageSize),
        page,
        pageSize,
        totalCount: count,
      });
      const occurredAt = (index: number) => new Date(Date.UTC(2026, 9, 14, 12, 0) - index * 15 * 60_000).toISOString();
      const securityCount = activeScenario === "empty" ? 0 : 60;
      const authenticationCount = activeScenario === "empty" ? 0 : 40;
      return HttpResponse.json({
        user: { id: userId, userName: "mock.user", firstName: "Mock", lastName: "User", email: "mock.user@example.test", phoneNumber: null, fullName: "Mock User", revision: "mock-revision", isEnabled: true },
        globalRoles: [],
        permittedGlobalRoles: ["PlatformAdmin"],
        tenantRoles: store.memberships.filter((membership) => membership.userId === "mock-user").map(({ userId: _userId, ...membership }) => membership),
        securityHistory: history(securityCount, securityPage, (index) => ({
          eventName: index % 2 === 0 ? "users.security-attributes.changed" : "users.account.edited",
          eventCategory: "security-attributes",
          outcome: index % 5 === 4 ? "denied" : "succeeded",
          actorId: "mock-platform-admin",
          operation: index % 2 === 0 ? "users.security-attributes.change" : "users.account.edit",
          occurredAt: occurredAt(index),
          clientInterfaceHost: "admin.localhost",
          isAuthenticationEvent: false,
          redactedReason: index % 5 === 4 ? "mutation_rejected" : null,
        })),
        authenticationHistory: history(authenticationCount, authenticationPage, (index) => ({
          eventName: "authentication.password.login",
          eventCategory: "authentication",
          outcome: index % 3 === 2 ? "denied" : "succeeded",
          actorId: index % 3 === 2 ? null : userId,
          operation: "password.login",
          occurredAt: occurredAt(index),
          clientInterfaceHost: "app.localhost",
          isAuthenticationEvent: true,
          redactedReason: index % 3 === 2 ? "authentication_failed" : null,
        })),
        securityHistoryAvailable: true,
        authenticationHistoryAvailable: true,
      });
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
      if (path.includes("/tenant-roles") && (request.method === "PUT" || request.method === "POST")) {
        const parts = path.split("/");
        const roleSuffix = parts.at(-1) ?? "TenantUser";
        const tenantId = parts.at(-2) ?? "mock-tenant";
        const userId = parts.at(-4) ?? "mock-user";
        const membership = { userId, tenantId, roleSuffix };
        const existing = store.memberships.findIndex((item) => item.userId === userId && item.tenantId === tenantId && item.roleSuffix === roleSuffix);
        if (existing < 0) store.memberships.push(membership);
        return HttpResponse.json(membership, { status: 201 });
      }
      if (path.includes("/tenant-roles/") && request.method === "DELETE") {
        const parts = path.split("/");
        const roleSuffix = parts.at(-1) ?? "TenantUser";
        const tenantId = parts.at(-2) ?? "mock-tenant";
        const userId = parts.at(-4) ?? "mock-user";
        const index = store.memberships.findIndex((item) => item.userId === userId && item.tenantId === tenantId && item.roleSuffix === roleSuffix);
        if (index < 0) return notFound("Membership does not exist in the mock store.");
        store.memberships.splice(index, 1);
        return new HttpResponse(null, { status: 204 });
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
          const wrapperKey = resource === "service-providers" ? "serviceProvider" : resource === "service-types" ? "serviceType" : resource.slice(0, -1);
          const source = body && typeof body[wrapperKey] === "object" && body[wrapperKey] !== null ? body[wrapperKey] as Record<string, unknown> : body ?? {};
          const created = { ...source, id: typeof source.id === "string" ? source.id : store.nextId("mock-appointment"), createdAt: "2026-01-02T00:00:00.000Z" };
          store.appointments[resource].push(created);
          return HttpResponse.json({ succeeded: true, entityId: created.id }, { status: 201 });
        }
      }
      return HttpResponse.json({ succeeded: true, entityId: "mock-appointment-entity" });
    }),
    ...createAuthMockHandlers({ isUnavailable: activeScenario === "error", withDelay, unavailable }),
    ...createSeaMockHandlers({ store, isUnavailable: activeScenario === "error", withDelay, unavailable, notFound }),
    ...createSurgeryMockHandlers({ isUnavailable: activeScenario === "error", withDelay, unavailable, notFound }),
    http.post(/\/api\/modules\/medical\/providers\/available(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return HttpResponse.json(activeScenario === "empty" ? [] : [{ providerKey: "Tamin" }]);
    }),
    http.get(/\/api\/modules\/medical\/credentials\/setup-context(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return HttpResponse.json({ doctorId: "mock-doctor", clinicId: "mock-clinic" });
    }),
    http.post(/\/api\/modules\/medical\/credentials\/setup-grid(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      if (activeScenario === "empty") return HttpResponse.json([]);
      return HttpResponse.json([{
        credentialId: taminCredentialId,
        doctorId: "mock-doctor",
        doctorName: "Dr. Ada Lovelace",
        clinicId: "mock-clinic",
        providerKey: "TAMIN",
        providerDoctorIdentifier: "ADA-001",
        providerUsername: "0012345678",
        providerClinicIdentifier: "CLINIC-001",
        expiresAt: "2026-12-31T00:00:00.000Z",
        daysUntilExpiry: 146,
        isExpiringSoon: false,
        lastTestedAt: taminAuthorized ? "2026-08-07T10:00:00.000Z" : null,
        status: taminAuthorized ? 1 : 0,
      }]);
    }),
    http.post(/\/api\/modules\/medical\/credentials(?:\?|$)/, async ({ request }) => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      const body = await request.json().catch(() => null) as Record<string, unknown> | null;
      if (!body || body.providerKey !== "TAMIN") return badRequest("TAMIN provider configuration is required.");
      taminCredentialId = typeof body.credentialId === "string" && body.credentialId ? body.credentialId : taminCredentialId;
      return HttpResponse.json({ accepted: true, isSuccess: true, entityId: taminCredentialId });
    }),
    http.post(/\/api\/modules\/medical\/credentials\/authorization\/begin(?:\?|$)/, async ({ request }) => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      const body = await request.json().catch(() => null) as { credentialId?: unknown } | null;
      if (body?.credentialId !== taminCredentialId) return badRequest("Unknown credential.");
      return HttpResponse.json({ isSuccess: true, authorizationUri: `https://tamin.mock.local/authorize?code=mock-code&state=mock-state&credentialId=${encodeURIComponent(taminCredentialId)}` });
    }),
    http.post(/\/api\/modules\/medical\/credentials\/authorization\/complete(?:\?|$)/, async ({ request }) => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      const body = await request.json().catch(() => null) as { credentialId?: unknown; authorizationCode?: unknown; state?: unknown } | null;
      if (body?.credentialId !== taminCredentialId || body.authorizationCode !== "mock-code" || body.state !== "mock-state") return badRequest("Authorization callback is invalid.");
      taminAuthorized = true;
      return HttpResponse.json({ accepted: true, isSuccess: true, entityId: taminCredentialId });
    }),
    http.post(/\/api\/modules\/medical\/credentials\/test(?:\?|$)/, async ({ request }) => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      const body = await request.json().catch(() => null) as { credentialId?: unknown } | null;
      if (body?.credentialId !== taminCredentialId) return badRequest("Unknown credential.");
      taminAuthorized = true;
      return HttpResponse.json({ accepted: true, isSuccess: true, entityId: taminCredentialId, value: "Credential token validated" });
    }),
    http.post(/\/api\/modules\/clinic\/visits\/prescription-drafts\/start(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return HttpResponse.json({ eventId: "mock-prescription-event", prescriptionId: "mock-prescription" }, { status: 201 });
    }),
    http.post(/\/api\/modules\/medical\/prescriptions\/reference-codes\/search(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return HttpResponse.json(activeScenario === "empty" ? [] : [{ providerKey: "Tamin", codeSystem: "Tamin", prescriptionCategory: 0, referenceCode: "AMOX-500", displayName: "Amoxicillin 500 mg", providerCategory: "Medication", metadata: {} }]);
    }),
    http.post(/\/api\/modules\/medical\/prescription-templates\/list(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return HttpResponse.json(activeScenario === "empty" ? [] : [{ id: "mock-template", name: "Routine acute visit" }]);
    }),
    // No prescriptions have been submitted in the mock practice.
    http.post(/\/api\/modules\/medical\/prescriptions\/history(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return HttpResponse.json([]);
    }),
    http.post(/\/api\/modules\/medical\/prescriptions\/[^/]+\/items(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return HttpResponse.json({ accepted: true, isSuccess: true, entityId: "mock-prescription-item" }, { status: 201 });
    }),
    http.post(/\/api\/modules\/medical\/prescriptions\/[^/]+\/validation(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return HttpResponse.json({ isValid: true, errors: [], warnings: [] });
    }),
    http.post(/\/api\/modules\/medical\/prescriptions\/[^/]+\/submission(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return HttpResponse.json({ accepted: true, isSuccess: true, entityId: "mock-prescription" });
    }),
    http.get(/\/api\/modules\/clinic\/(?:patients\/[^/]+\/assistant|assistant)\/actions\/[^/]+(?:\?|$)/, async () => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return HttpResponse.json({
        actionId: "mock-agent-action",
        agentRunId: "mock-agent-run",
        conversationId: "mock-agent-conversation",
        capability: "Appointment scheduling",
        actionType: "create_appointment",
        status: "Proposed",
        reviewPayload: { slot: { date: "2026-08-12", startTime: "10:30", durationMinutes: 30, service: "Initial consultation", practitioner: "Dr. Ada Lovelace" }, appointmentType: "Consultation", description: "Follow-up appointment proposed by the patient assistant.", availabilityWarnings: [] },
        expiresAt: "2026-08-12T09:30:00.000Z",
        completionPayload: {},
        failure: null,
      });
    }),
    http.post(/\/api\/modules\/clinic\/(?:patients\/[^/]+\/assistant|assistant)\/actions\/[^/]+\/(confirm|reject)(?:\?|$)/, async ({ request }) => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      return HttpResponse.json({ actionId: "mock-agent-action", status: request.url.endsWith("/confirm") ? "Completed" : "Rejected", completionPayload: request.url.endsWith("/confirm") ? { appointmentId: "mock-reservation" } : {}, failure: null });
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
    http.post(/\/api\/modules\/intelligence\/agent-conversations(?:\?|$)/, async ({ request }) => {
      await withDelay();
      if (activeScenario === "error") return unavailable();
      const body = await request.json().catch(() => null) as { agentKey?: unknown } | null;
      const isClinic = body?.agentKey === "clinic.assistant.patient";
      return HttpResponse.json({
        agentRunId: "mock-agent-run",
        conversationId: "mock-agent-conversation",
        result: isClinic ? {
          agentRunId: "mock-agent-run",
          conversationId: "mock-agent-conversation",
          previousResponseId: null,
          messages: [{ kind: "AssistantMessage", content: "I found a suitable appointment slot. Review the proposed action before confirming it." }],
          actions: [{ id: "mock-agent-action", capability: "Appointment scheduling", actionType: "create_appointment", status: "Proposed", arguments: {}, summary: "Book a 30-minute consultation with Dr. Ada Lovelace." }],
        } : { text: "Deterministic mock agent result." },
      });
    }),
  ];
}

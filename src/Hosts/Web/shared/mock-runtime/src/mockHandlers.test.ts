import { setupServer } from "msw/node";
import { afterAll, afterEach, beforeAll, beforeEach, describe, expect, it } from "vitest";
import { createDefaultMockHandlers, resolveMockScenario } from "./mockHandlers";
import { mockAnonymousSessionValue, mockSessionCookieName } from "./mockSession";

const server = setupServer(...createDefaultMockHandlers("ready"));

beforeAll(() => server.listen({ onUnhandledRequest: "error" }));
beforeEach(() => server.resetHandlers(...createDefaultMockHandlers("ready")));
afterEach(() => server.resetHandlers());
afterAll(() => server.close());

describe("mock preview handlers", () => {
  it("uses ready for an invalid scenario", () => {
    expect(resolveMockScenario("unexpected")).toBe("ready");
    expect(resolveMockScenario("empty")).toBe("empty");
  });

  it("answers as a signed-out visitor when the mock session cookie asks for it", async () => {
    const session = await fetch("https://mock.example.test/api/modules/authentication/session", {
      headers: { cookie: `${mockSessionCookieName}=${mockAnonymousSessionValue}` },
    });

    await expect(session.json()).resolves.toMatchObject({ isAuthenticated: false, user: null });
  });

  it("enables every sign-in method in the authentication profile", async () => {
    const profile = await fetch("https://mock.example.test/api/modules/authentication/profile");

    await expect(profile.json()).resolves.toEqual({
      allowPasswordlessPhoneLogin: true,
      allowPasswordlessEmailLogin: true,
      allowPasskeyLogin: true,
    });
  });

  it("returns a session and antiforgery token through the normal HTTP boundary", async () => {
    const session = await fetch("https://mock.example.test/api/modules/authentication/session");
    const antiforgery = await fetch("https://mock.example.test/api/modules/authentication/antiforgery");

    expect(session.status).toBe(200);
    await expect(session.json()).resolves.toMatchObject({ isAuthenticated: true, canAccessPlatformAdmin: true, user: { userId: "mock-user" } });
    await expect(antiforgery.json()).resolves.toMatchObject({ requestToken: "mock-antiforgery-token" });
  });

  it("returns an empty knowledge workspace for the empty scenario", async () => {
    server.use(...createDefaultMockHandlers("empty"));

    const response = await fetch("https://mock.example.test/api/modules/knowledge/workspace");

    await expect(response.json()).resolves.toEqual({ corpora: [] });
  });

  it("returns an inspectable patient list and count for Clinic routes", async () => {
    const [patients, count] = await Promise.all([
      fetch("https://mock.example.test/api/modules/clinic/patients", { method: "POST", body: "{}" }),
      fetch("https://mock.example.test/api/modules/clinic/patients/count", { method: "POST", body: "{}" }),
    ]);

    await expect(patients.json()).resolves.toMatchObject([
      { id: "mock-patient-ada", name: "Ada Lovelace" },
      { id: "mock-patient-grace", name: "Grace Hopper" },
      { id: "mock-patient-katherine", name: "Katherine Johnson" },
      { id: "mock-patient-marie", name: "Marie Curie" },
    ]);
    await expect(count.json()).resolves.toBe(4);
  });

  it("keeps Knowledge answers deterministic but workflow-aware", async () => {
    const first = await fetch("https://mock.example.test/api/modules/knowledge/ask", { method: "POST", headers: { "content-type": "application/json" }, body: JSON.stringify({ question: "First question" }) });
    const second = await fetch("https://mock.example.test/api/modules/knowledge/ask", { method: "POST", headers: { "content-type": "application/json" }, body: JSON.stringify({ question: "Second question" }) });
    await expect(first.json()).resolves.toMatchObject({ answer: expect.stringContaining("mock answer 1") });
    await expect(second.json()).resolves.toMatchObject({ answer: expect.stringContaining("mock answer 2") });
  });

  it("persists a patient workflow mutation for later reads", async () => {
    const created = await fetch("https://mock.example.test/api/modules/clinic/patients/find-or-create", {
      method: "POST", headers: { "content-type": "application/json" }, body: JSON.stringify({ firstName: "Grace", lastName: "Hopper" }),
    });
    expect(created.status).toBe(201);
    const patient = await created.json() as { id: string; name: string };
    const list = await fetch("https://mock.example.test/api/modules/clinic/patients", { method: "POST", body: "{}" });
    await expect(list.json()).resolves.toEqual(expect.arrayContaining([expect.objectContaining({ id: patient.id, name: "Grace Hopper" })]));
  });

  it("updates a patient by its URL identifier", async () => {
    const response = await fetch("https://mock.example.test/api/modules/clinic/patients/mock-patient-ada", {
      method: "PUT", headers: { "content-type": "application/json" }, body: JSON.stringify({ firstName: "Ada", lastName: "Byron" }),
    });
    expect(response.status).toBe(200);
    await expect(response.json()).resolves.toMatchObject({ id: "mock-patient-ada", name: "Ada Byron" });
  });

  it("returns platform administration rows for operator screens", async () => {
    const [owners, tenants, users] = await Promise.all([
      fetch("https://mock.example.test/api/modules/parties/administration/owners"),
      fetch("https://mock.example.test/api/modules/parties/administration/tenants"),
      fetch("https://mock.example.test/api/modules/users/search?Query=mock"),
    ]);

    await expect(owners.json()).resolves.toMatchObject([{ id: "mock-owner" }]);
    await expect(tenants.json()).resolves.toMatchObject([{ id: "mock-tenant" }]);
    await expect(users.json()).resolves.toMatchObject([{ userId: "mock-user" }]);
  });

  it("returns platform detail and membership fixtures", async () => {
    const [ownerTenants, indicators, roles] = await Promise.all([
      fetch("https://mock.example.test/api/modules/parties/administration/owners/mock-owner/tenants"),
      fetch("https://mock.example.test/api/modules/parties/administration/tenants/mock-tenant/indicators"),
      fetch("https://mock.example.test/api/modules/users/administration/mock-user/tenant-roles"),
    ]);

    await expect(ownerTenants.json()).resolves.toMatchObject([{ id: "mock-tenant" }]);
    await expect(indicators.json()).resolves.toMatchObject([{ value: "mock.localhost" }]);
    await expect(roles.json()).resolves.toMatchObject([{ tenantId: "mock-tenant", roleSuffix: "Owner" }]);
  });

  it("returns a realistic paged audit catalog and filters records by operator fields", async () => {
    const catalog = await fetch("https://mock.example.test/api/audit/platform/security-event-catalog");
    const filtered = await fetch("https://mock.example.test/api/audit/platform/security-events?page=1&pageSize=25&eventCategory=authentication&eventName=authentication.password.login&operation=password.login&outcome=denied&sortBy=1&descending=true&uncataloguedOnly=false");

    const catalogBody = await catalog.json() as Array<{ eventName: string }>;
    const filteredBody = await filtered.json() as { items: Array<{ eventName: string; eventCategory: string; operation: string; outcome: string }>; totalCount: number };
    expect(catalogBody.some((entry) => entry.eventName === "authentication.password.login")).toBe(true);
    expect(filteredBody.totalCount).toBeGreaterThan(0);
    expect(filteredBody.items.every((event) => event.eventName === "authentication.password.login" && event.eventCategory === "authentication" && event.operation === "password.login" && event.outcome === "denied")).toBe(true);
  });

  it("exports the same safe audit projection used by the mock review grid", async () => {
    const response = await fetch("https://mock.example.test/api/audit/platform/security-events/export?eventCategory=authentication&outcome=denied&sortBy=1&descending=true&uncataloguedOnly=false");
    const csv = await response.text();

    expect(response.headers.get("content-type")).toContain("text/csv");
    expect(csv).toContain("occurredAt,tenantId,eventCategory,eventName,operation,outcome");
    expect(csv).toContain("authentication.password.login");
    expect(csv).not.toContain("E2E-Audit");
  });

  it("persists an owner create workflow for later reads", async () => {
    const created = await fetch("https://mock.example.test/api/modules/parties/administration/owners", {
      method: "POST", headers: { "content-type": "application/json" }, body: JSON.stringify({ id: "mock-owner-new", name: "New Practice" }),
    });
    expect(created.status).toBe(201);
    const owners = await fetch("https://mock.example.test/api/modules/parties/administration/owners");
    await expect(owners.json()).resolves.toEqual(expect.arrayContaining([expect.objectContaining({ id: "mock-owner-new", name: "New Practice" })]));
  });

  it("applies role grants and revokes using the generated client methods", async () => {
    const path = "https://mock.example.test/api/modules/users/administration/mock-user/tenant-roles/mock-tenant/TenantUser";
    const grant = await fetch(path, { method: "PUT", headers: { "content-type": "application/json" }, body: JSON.stringify({ roleSuffix: "TenantUser" }) });
    expect(grant.status).toBe(201);
    const roles = await fetch("https://mock.example.test/api/modules/users/administration/mock-user/tenant-roles");
    await expect(roles.json()).resolves.toEqual(expect.arrayContaining([expect.objectContaining({ roleSuffix: "TenantUser" })]));
    const revoke = await fetch(path, { method: "DELETE" });
    expect(revoke.status).toBe(204);
  });

  it("supports the public landing conversation HTTP contract", async () => {
    const response = await fetch("https://mock.example.test/api/apps/landing/conversations", {
      method: "POST",
      headers: { "content-type": "application/json" },
      body: JSON.stringify({ ownerKey: "home", message: "How does this work?", conversationId: null }),
    });

    await expect(response.json()).resolves.toMatchObject({ message: "Mock guidance for: How does this work?" });
  });

  it("keeps landing conversation identity across messages and deletes it on reset", async () => {
    const first = await fetch("https://mock.example.test/api/apps/landing/conversations", { method: "POST", headers: { "content-type": "application/json" }, body: JSON.stringify({ ownerKey: "home", message: "First", conversationId: null }) });
    const firstBody = await first.json() as { conversationId: string };
    const second = await fetch("https://mock.example.test/api/apps/landing/conversations", { method: "POST", headers: { "content-type": "application/json" }, body: JSON.stringify({ ownerKey: "home", message: "Second", conversationId: firstBody.conversationId }) });
    await expect(second.json()).resolves.toMatchObject({ conversationId: firstBody.conversationId });
    const reset = await fetch(`https://mock.example.test/api/apps/landing/conversations/${firstBody.conversationId}`, { method: "DELETE" });
    expect(reset.status).toBe(204);
  });

  it("returns appointment fixtures through the handwritten Clinic client shape", async () => {
    const response = await fetch("https://mock.example.test/api/modules/appointments/services", {
      method: "POST",
      headers: { "content-type": "application/json" },
      body: JSON.stringify({ descriptor: {} }),
    });

    await expect(response.json()).resolves.toMatchObject([{ id: "mock-service", name: "Initial consultation" }]);
  });

  it("supports the deterministic Tamin credential authorization lifecycle", async () => {
    const context = await fetch("https://mock.example.test/api/modules/medical/credentials/setup-context");
    await expect(context.json()).resolves.toEqual({ doctorId: "mock-doctor", clinicId: "mock-clinic" });

    const configured = await fetch("https://mock.example.test/api/modules/medical/credentials", {
      method: "POST", headers: { "content-type": "application/json" },
      body: JSON.stringify({ providerKey: "TAMIN", credentialId: null }),
    });
    const configuredBody = await configured.json() as { entityId: string };
    expect(configuredBody.entityId).toBe("mock-tamin-credential");

    const begin = await fetch("https://mock.example.test/api/modules/medical/credentials/authorization/begin", {
      method: "POST", headers: { "content-type": "application/json" },
      body: JSON.stringify({ credentialId: configuredBody.entityId }),
    });
    await expect(begin.json()).resolves.toMatchObject({ isSuccess: true, authorizationUri: expect.stringContaining("mock-code") });

    const complete = await fetch("https://mock.example.test/api/modules/medical/credentials/authorization/complete", {
      method: "POST", headers: { "content-type": "application/json" },
      body: JSON.stringify({ credentialId: configuredBody.entityId, authorizationCode: "mock-code", state: "mock-state" }),
    });
    await expect(complete.json()).resolves.toMatchObject({ accepted: true, entityId: configuredBody.entityId });

    const tested = await fetch("https://mock.example.test/api/modules/medical/credentials/test", {
      method: "POST", headers: { "content-type": "application/json" },
      body: JSON.stringify({ credentialId: configuredBody.entityId }),
    });
    await expect(tested.json()).resolves.toMatchObject({ accepted: true, value: "Credential token validated" });

    const grid = await fetch("https://mock.example.test/api/modules/medical/credentials/setup-grid", {
      method: "POST", headers: { "content-type": "application/json" }, body: JSON.stringify({ clinicId: "mock-clinic", providerKey: "TAMIN" }),
    });
    await expect(grid.json()).resolves.toMatchObject([{ status: 1, providerKey: "TAMIN" }]);
  });

  it("persists an appointment resource create for later list reads", async () => {
    const created = await fetch("https://mock.example.test/api/modules/appointments/services/create", {
      method: "POST", headers: { "content-type": "application/json" }, body: JSON.stringify({ service: { id: "mock-service-new", name: "Follow-up" } }),
    });
    expect(created.status).toBe(201);
    await expect(created.json()).resolves.toMatchObject({ succeeded: true, entityId: "mock-service-new" });
    const services = await fetch("https://mock.example.test/api/modules/appointments/services", { method: "POST", body: "{}" });
    await expect(services.json()).resolves.toEqual(expect.arrayContaining([expect.objectContaining({ id: "mock-service-new", name: "Follow-up" })]));
  });

  it("rejects malformed patient commands without changing the seed", async () => {
    const invalid = await fetch("https://mock.example.test/api/modules/clinic/patients/find-or-create", { method: "POST", headers: { "content-type": "application/json" }, body: JSON.stringify({ firstName: "Only" }) });
    expect(invalid.status).toBe(400);
    const count = await fetch("https://mock.example.test/api/modules/clinic/patients/count", { method: "POST", body: "{}" });
    await expect(count.json()).resolves.toBe(4);
  });

  it("returns deterministic Intelligence capabilities and agent results", async () => {
    const [capabilities, agent] = await Promise.all([
      fetch("https://mock.example.test/api/modules/intelligence/transcription/live/capabilities"),
      fetch("https://mock.example.test/api/modules/intelligence/agent-conversations", { method: "POST", body: "{}" }),
    ]);

    await expect(capabilities.json()).resolves.toMatchObject({ available: false, languages: ["en-US", "fa-IR"] });
    await expect(agent.json()).resolves.toMatchObject({ agentRunId: "mock-agent-run", conversationId: "mock-agent-conversation" });
  });

  it("keeps SEA event order and derives procedure progress from its events and items", async () => {
    const sea = "https://mock.example.test/api/modules/sea";
    const post = (path: string, body?: unknown) => fetch(`${sea}/${path}`, { method: "POST", body: JSON.stringify(body ?? {}) }).then((response) => response.json());
    const procedure = await post("procedures", { projectId: "mock-patient-grace", name: "Check-up" });
    const first = await post("events/after", { projectId: "mock-patient-grace", procedureId: procedure.entityId, title: "Examination" });
    const second = await post(`events/after/${first.entityId}`, { projectId: "mock-patient-grace", procedureId: procedure.entityId, title: "Cleaning" });

    await post(`events/${first.entityId}/commit`);
    await post("attachments/text", { eventId: first.entityId, content: "Gums healthy.", contentType: "text/plain", size: 13, kind: 0 });

    const events = await fetch(`${sea}/procedures/${procedure.entityId}/events`).then((response) => response.json());
    const detail = await fetch(`${sea}/procedures/${procedure.entityId}`).then((response) => response.json());
    expect(events.map((event: { title: string; previousEventId: string | null; isCommitted: boolean }) => [event.title, event.previousEventId, event.isCommitted]))
      .toEqual([["Examination", null, true], ["Cleaning", first.entityId, false]]);
    expect(second).toMatchObject({ succeeded: true });
    expect(detail.stats).toMatchObject({ events: 2, committedEvents: 1, eventItems: 1, state: "Half" });
  });
});

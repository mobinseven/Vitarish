import { setupServer } from "msw/node";
import { afterAll, afterEach, beforeAll, describe, expect, it } from "vitest";
import { createDefaultMockHandlers, resolveMockScenario } from "./mockHandlers";

const server = setupServer(...createDefaultMockHandlers("ready"));

beforeAll(() => server.listen({ onUnhandledRequest: "error" }));
afterEach(() => server.resetHandlers());
afterAll(() => server.close());

describe("mock preview handlers", () => {
  it("uses ready for an invalid scenario", () => {
    expect(resolveMockScenario("unexpected")).toBe("ready");
    expect(resolveMockScenario("empty")).toBe("empty");
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

    await expect(patients.json()).resolves.toMatchObject([{ id: "mock-patient-ada", name: "Ada Lovelace" }]);
    await expect(count.json()).resolves.toBe(1);
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

  it("supports the public landing conversation HTTP contract", async () => {
    const response = await fetch("https://mock.example.test/api/apps/landing/conversations", {
      method: "POST",
      headers: { "content-type": "application/json" },
      body: JSON.stringify({ ownerKey: "home", message: "How does this work?", conversationId: null }),
    });

    await expect(response.json()).resolves.toMatchObject({
      conversationId: "mock-landing-conversation",
      message: "Mock guidance for: How does this work?",
    });
  });

  it("returns appointment fixtures through the handwritten Clinic client shape", async () => {
    const response = await fetch("https://mock.example.test/api/modules/appointments/services", {
      method: "POST",
      headers: { "content-type": "application/json" },
      body: JSON.stringify({ descriptor: {} }),
    });

    await expect(response.json()).resolves.toMatchObject([{ id: "mock-service", name: "Initial consultation" }]);
  });

  it("returns deterministic Intelligence capabilities and agent results", async () => {
    const [capabilities, agent] = await Promise.all([
      fetch("https://mock.example.test/api/modules/intelligence/transcription/live/capabilities"),
      fetch("https://mock.example.test/api/modules/intelligence/agent-conversations", { method: "POST", body: "{}" }),
    ]);

    await expect(capabilities.json()).resolves.toMatchObject({ available: false, languages: ["en-US", "fa-IR"] });
    await expect(agent.json()).resolves.toMatchObject({ agentRunId: "mock-agent-run", conversationId: "mock-agent-conversation" });
  });
});

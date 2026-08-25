export type MockOwner = { id: string; name: string; description?: string };
export type MockTenant = { id: string; name: string; appId: string; ownerId: string };
export type MockIndicator = { id: string; tenantId: string; value: string; method: number; status: number; isPrimary: boolean };
export type MockUser = { userId: string; fullName: string; userName: string; email?: string };
export type MockMembership = { tenantId: string; roleSuffix: string };
export type MockPatient = { id: string; tenantId: string; name: string; firstName: string; lastName: string; identificationCode: string; phone?: string; email?: string; dateOfBirth?: string; createdAt: string; isDeleted: boolean };
export type MockConversation = { id: string; ownerKey: string; messages: Array<{ role: "user" | "assistant"; text: string }> };

export function createMockStore(scenario: "ready" | "empty") {
  let sequence = 1;
  const store = {
    owners: scenario === "empty" ? [] as MockOwner[] : [{ id: "mock-owner", name: "Mock Practice", description: "Deterministic mock owner." }],
    tenants: scenario === "empty" ? [] as MockTenant[] : [{ id: "mock-tenant", name: "Mock Tenant", appId: "private-practice-owner", ownerId: "mock-owner" }],
    indicators: scenario === "empty" ? [] as MockIndicator[] : [{ id: "mock-indicator", tenantId: "mock-tenant", value: "mock.localhost", method: 1, status: 3, isPrimary: true }],
    users: scenario === "empty" ? [] as MockUser[] : [{ userId: "mock-user", fullName: "Mock User", userName: "mock.user", email: "mock.user@example.test" }],
    memberships: scenario === "empty" ? [] as Array<MockMembership & { userId: string }>: [{ userId: "mock-user", tenantId: "mock-tenant", roleSuffix: "Owner" }],
    patients: scenario === "empty" ? [] as MockPatient[] : [
      { id: "mock-patient-ada", tenantId: "mock-tenant", name: "Ada Lovelace", firstName: "Ada", lastName: "Lovelace", identificationCode: "mock-ada-001", phone: "+10000000001", email: "ada@example.test", dateOfBirth: "1990-12-10", createdAt: "2026-01-01T00:00:00.000Z", isDeleted: false },
      { id: "mock-patient-grace", tenantId: "mock-tenant", name: "Grace Hopper", firstName: "Grace", lastName: "Hopper", identificationCode: "mock-grace-002", phone: "+10000000002", email: "grace@example.test", dateOfBirth: "1985-12-09", createdAt: "2026-01-02T00:00:00.000Z", isDeleted: false },
      { id: "mock-patient-katherine", tenantId: "mock-tenant", name: "Katherine Johnson", firstName: "Katherine", lastName: "Johnson", identificationCode: "mock-katherine-003", phone: "+10000000003", email: "katherine@example.test", dateOfBirth: "1988-08-26", createdAt: "2026-01-03T00:00:00.000Z", isDeleted: false },
      { id: "mock-patient-marie", tenantId: "mock-tenant", name: "Marie Curie", firstName: "Marie", lastName: "Curie", identificationCode: "mock-marie-004", phone: "+10000000004", email: "marie@example.test", dateOfBirth: "1992-11-07", createdAt: "2026-01-04T00:00:00.000Z", isDeleted: false },
    ],
    conversations: [] as MockConversation[],
    knowledgeAsks: [] as string[],
    appointments: {
      reservations: scenario === "empty" ? [] as Record<string, unknown>[] : [{ id: "mock-reservation", createdAt: "2026-01-01T00:00:00.000Z", isDeleted: false, status: 1, shiftId: "mock-shift", date: "2026-08-11", isHold: false }],
      shifts: scenario === "empty" ? [] as Record<string, unknown>[] : [{ id: "mock-shift", createdAt: "2026-01-01T00:00:00.000Z", isDeleted: false, status: 1, visibility: 1, dayOfWeek: 2, startTime: "10:30", endTime: "11:00", timeZone: "UTC", totalSeats: 1, serviceProviderId: "mock-provider", serviceId: "mock-service" }],
      "service-types": scenario === "empty" ? [] as Record<string, unknown>[] : [{ id: "mock-service-type", createdAt: "2026-01-01T00:00:00.000Z", isDeleted: false, name: "Consultation", description: "Mock consultation" }],
      services: scenario === "empty" ? [] as Record<string, unknown>[] : [{ id: "mock-service", createdAt: "2026-01-01T00:00:00.000Z", isDeleted: false, name: "Initial consultation", description: "Mock service", serviceTypeId: "mock-service-type", serviceTypeName: "Consultation" }],
      "service-providers": scenario === "empty" ? [] as Record<string, unknown>[] : [{ id: "mock-provider", createdAt: "2026-01-01T00:00:00.000Z", isDeleted: false, name: "Mock clinician", description: "Mock provider", linkedUserIds: ["mock-user"] }],
    },
    nextId(prefix: string) { return `${prefix}-${sequence++}`; },
  };
  return store;
}

export type MockStore = ReturnType<typeof createMockStore>;

export type MockOwner = { id: string; name: string; description?: string };
export type MockTenant = { id: string; name: string; appId: string; ownerId: string };
export type MockIndicator = { id: string; tenantId: string; value: string; method: number; status: number; isPrimary: boolean };
export type MockUser = { userId: string; fullName: string; userName: string; email?: string };
export type MockMembership = { tenantId: string; roleSuffix: string };
export type MockPatient = { id: string; tenantId: string; name: string; firstName: string; lastName: string; identificationCode: string; phone?: string; email?: string; dateOfBirth?: string; createdAt: string; isDeleted: boolean };
export type MockConversation = { id: string; ownerKey: string; messages: Array<{ role: "user" | "assistant"; text: string }> };
export type MockAuditCatalogEntry = { category: string; eventName: string; operation: string; isWildcard: boolean };
export type MockAuditEvent = {
  id: string;
  eventName: string;
  eventCategory: string;
  outcome: "started" | "succeeded" | "denied" | "pending" | "failed";
  actorKind: "anonymous" | "user" | "system";
  actorId: string;
  subjectId?: string;
  tenantId?: string;
  resource: string;
  operation: string;
  occurredAt: string;
  correlationId: string;
  sourceBoundary: string;
  sourceIpKind: "direct" | "forwarded";
  sourceIpAddress: string;
  redactedReason?: string;
};

// Keep mock values in the shared fixture owner so every mock host exercises the
// same operator-facing catalog and realistic, mixed-outcome audit history.
export const mockAuditCatalog: MockAuditCatalogEntry[] = [
  ["function", "security.endpoint.mutation", "*"],
  ["audit-review", "security.audit.review", "audit.security-events.list"],
  ["audit-configuration", "security.audit.configuration", "audit.configuration.read"],
  ["security-failure", "security.audit.storage-threshold", "audit.storage.threshold"],
  ["authentication", "authentication.account.password.change", "account.password.change"],
  ["authentication", "authentication.password.login", "password.login"],
  ["authentication", "authentication.password.login.otp", "password.login.otp"],
  ["authentication", "authentication.identity.password-sign-in.user", "identity.password.sign-in.user"],
  ["authentication", "authentication.identity.password-sign-in.name", "identity.password.sign-in.name"],
  ["authentication", "authentication.identity.password-check", "identity.password.check"],
  ["authentication", "authentication.identity.external-login", "identity.external.login"],
  ["authentication", "authentication.identity.mfa.authenticator", "identity.mfa.authenticator"],
  ["authentication", "authentication.identity.mfa.recovery-code", "identity.mfa.recovery-code"],
  ["authentication", "authentication.identity.mfa.provider", "identity.mfa.provider"],
  ["authentication", "authentication.security-access-policy", "Authentication_SecurityAccessPolicy_Change"],
  ["security-attributes", "authentication.security-attributes.changed", "security-attributes.change"],
  ["import-export", "security.data.import", "data.import"],
  ["import-export", "security.data.export", "data.export"],
  ["import-export", "security.data.integrity-verification", "data.integrity.verify"],
  ["management", "security.management.operation", "management.operation"],
  ["security-failure", "security.failure", "security.failure"],
  ["session", "authentication.session.established", "session.establish"],
  ["session", "authentication.session.terminated", "session.terminate"],
  ["session", "authentication.access-history", "access-history"],
  ["session", "authentication.session.notice", "session.notice"],
  ["session", "authentication.session-policy", "session-policy.change"],
  ["management", "users.tenant-role-replacement", "Users_ReplaceTenantRole"],
  ["management", "users.global-role-assignment", "Users_AssignGlobalRole"],
  ["management", "users.global-role-revocation", "Users_RevokeGlobalRole"],
  ["management", "users.tenant-member-provisioning", "Users_ProvisionTenantMember"],
  ["management", "users.tenant-member-removal", "Users_RemoveTenantMember"],
].map(([category, eventName, operation]) => ({ category, eventName, operation, isWildcard: operation === "*" }));

function createMockAuditEvents(): MockAuditEvent[] {
  const repeated = [
    ["authentication.password.login", "authentication", "password.login", "anonymous", "denied"],
    ["authentication.identity.password-check", "authentication", "identity.password.check", "anonymous", "succeeded"],
    ["authentication.password.login", "authentication", "password.login", "anonymous", "started"],
    ["authentication.security-access-policy", "authentication", "Authentication_SecurityAccessPolicy_Change", "user", "succeeded"],
    ["authentication.security-access-policy", "authentication", "Authentication_SecurityAccessPolicy_Change", "user", "pending"],
    ["authentication.account.password.change", "authentication", "account.password.change", "user", "succeeded"],
    ["users.tenant-member-provisioning", "management", "Users_ProvisionTenantMember", "user", "succeeded"],
    ["security.data.export", "import-export", "data.export", "user", "succeeded"],
  ] as const;
  return Array.from({ length: 40 }, (_, index) => {
    const [eventName, eventCategory, operation, actorKind, outcome] = repeated[index % repeated.length];
    const offset = index * 11;
    return {
      id: `mock-audit-${String(index + 1).padStart(3, "0")}`,
      eventName,
      eventCategory,
      operation,
      outcome,
      actorKind,
      actorId: actorKind === "anonymous" ? "anonymous" : "mock-user",
      subjectId: actorKind === "anonymous" ? undefined : "mock-user",
      tenantId: index % 5 === 0 ? undefined : "mock-tenant",
      resource: eventCategory === "management" ? "tenant-membership" : "authentication",
      occurredAt: new Date(Date.UTC(2026, 8, 21, 11, 45, 0) - offset * 60_000).toISOString(),
      correlationId: `mock-correlation-${String(index + 1).padStart(3, "0")}`,
      sourceBoundary: actorKind === "anonymous" ? "main.endpoint-filter" : "authentication.application",
      sourceIpKind: actorKind === "anonymous" ? "direct" : "forwarded",
      sourceIpAddress: actorKind === "anonymous" ? "1::1" : index % 2 === 0 ? "198.51.100.62" : "198.51.100.61",
      redactedReason: actorKind === "anonymous" && outcome === "denied" ? "authentication failure" : undefined,
    };
  });
}

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
    auditCatalog: mockAuditCatalog,
    auditEvents: scenario === "empty" ? [] as MockAuditEvent[] : createMockAuditEvents(),
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

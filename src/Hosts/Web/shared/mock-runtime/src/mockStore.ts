import { tenantRoleSuffixes } from "@vitarish/auth/users";
import { RoleType } from "@vitarish/sea/api/generated/sea/model";

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
const mockSessionTerminatedAudit = {
  category: "session",
  eventName: "authentication.session.terminated",
  operation: "session.terminate",
} as const;

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
  [mockSessionTerminatedAudit.category, mockSessionTerminatedAudit.eventName, mockSessionTerminatedAudit.operation],
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
    [mockSessionTerminatedAudit.eventName, mockSessionTerminatedAudit.category, mockSessionTerminatedAudit.operation, "system", "succeeded"],
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
      actorId: actorKind === "anonymous" ? "anonymous" : actorKind === "system" ? "system" : "mock-user",
      subjectId: actorKind === "anonymous" ? undefined : "mock-user",
      tenantId: index % 5 === 0 ? undefined : "mock-tenant",
      resource: eventCategory === "management" ? "tenant-membership" : "authentication",
      occurredAt: new Date(Date.UTC(2026, 8, 21, 11, 45, 0) - offset * 60_000).toISOString(),
      correlationId: `mock-correlation-${String(index + 1).padStart(3, "0")}`,
      sourceBoundary: actorKind === "anonymous" ? "main.endpoint-filter" : "authentication.application",
      sourceIpKind: actorKind === "anonymous" ? "direct" : "forwarded",
      sourceIpAddress: actorKind === "anonymous" ? "1::1" : index % 2 === 0 ? "198.51.100.62" : "198.51.100.61",
      redactedReason: actorKind === "anonymous" && outcome === "denied"
        ? "authentication failure"
        : eventName === mockSessionTerminatedAudit.eventName
          ? "Session expired due to inactivity."
          : undefined,
    };
  });
}

/** Ids of seeded ready-scenario records that previews and screenshot runs link to. */
export const mockFixtureIds = {
  patient: "mock-patient-ada",
  reservation: "mock-reservation",
  shift: "mock-shift",
  shiftOccurrenceDate: "2026-08-11",
  serviceType: "mock-service-type",
  service: "mock-service",
  serviceProvider: "mock-provider",
  seaTemplateProject: "mock-sea-template-project",
  seaTemplateProcedure: "mock-sea-template-procedure",
  seaProcedure: "mock-sea-procedure",
  seaEvent: "mock-sea-event",
  clinician: "mock-user-amini",
  assistant: "mock-user-karimi",
} as const;

// SEA records keep the server's read shape minus stats and event order, which the
// mock SEA handlers derive from these records the way the module does.
type MockSeaTracked = { id: string; createdAt: string; createdBy: string; updatedAt: string | null; isDeleted: boolean };
export type MockSeaProject = MockSeaTracked & { name: string; description: string; identificationCode: string | null; template: boolean; userIds: string[] };
export type MockSeaProcedure = MockSeaTracked & { projectId: string; name: string; description: string | null };
export type MockSeaEvent = MockSeaTracked & { projectId: string; procedureId: string; title: string; description: string; committedAt: string | null; committedBy: string; isCommitted: boolean };
export type MockSeaAttachment = MockSeaTracked & { eventId: string; fileName: string | null; content: string; contentType: string; size: number; kind: number };
export type MockSeaRole = MockSeaTracked & { name: string };
export type MockSeaEventRole = MockSeaTracked & { eventId: string; roleId: string; type: RoleType };

const mockSeaUser = "mock-user";
const tracked = (id: string, createdAt: string, createdBy = mockSeaUser): MockSeaTracked => ({ id, createdAt, createdBy, updatedAt: null, isDeleted: false });
// A small non-icon illustration so the attachment preview renders without the Files module.
const mockSeaImage = `data:image/svg+xml;utf8,${encodeURIComponent("<svg xmlns='http://www.w3.org/2000/svg' width='320' height='200'><rect width='320' height='200' fill='#1f2937'/><ellipse cx='160' cy='100' rx='110' ry='60' fill='#e5e7eb' opacity='.85'/><rect x='70' y='80' width='180' height='40' rx='16' fill='#9ca3af'/></svg>")}`;

function createMockSeaRecords() {
  const { patient, seaTemplateProject, seaTemplateProcedure, seaProcedure, seaEvent } = mockFixtureIds;
  const { clinician, assistant } = mockFixtureIds;
  const textNote = (id: string, eventId: string, content: string, createdAt: string, author = mockSeaUser): MockSeaAttachment => (
    { ...tracked(id, createdAt, author), eventId, fileName: null, content, contentType: "text/plain", size: content.length, kind: 0 }
  );
  const step = (id: string, procedureId: string, projectId: string, title: string, description: string, createdAt: string, committedAt: string | null = null): MockSeaEvent => (
    { ...tracked(id, createdAt), projectId, procedureId, title, description, committedAt, committedBy: committedAt ? mockSeaUser : "", isCommitted: committedAt !== null }
  );
  return {
    projects: [
      { ...tracked(seaTemplateProject, "2026-01-05T08:00:00.000Z"), name: "Orthodontic care", description: "Reusable orthodontic treatment plans.", identificationCode: null, template: true, userIds: [mockSeaUser] },
      { ...tracked("mock-sea-template-hygiene", "2026-01-06T08:00:00.000Z"), name: "Preventive care", description: "Hygiene and check-up routines.", identificationCode: null, template: true, userIds: [mockSeaUser] },
    ] as MockSeaProject[],
    procedures: [
      { ...tracked(seaTemplateProcedure, "2026-01-05T08:10:00.000Z"), projectId: seaTemplateProject, name: "Clear aligner treatment", description: "Assessment, impressions, fitting and review." },
      { ...tracked("mock-sea-template-cleaning", "2026-01-06T08:10:00.000Z"), projectId: "mock-sea-template-hygiene", name: "Six-month check-up", description: "Examination and professional cleaning." },
      { ...tracked("mock-sea-visits", "2026-02-01T09:00:00.000Z"), projectId: patient, name: "Visits", description: "" },
      { ...tracked(seaProcedure, "2026-02-03T09:00:00.000Z"), projectId: patient, name: "Clear aligner treatment", description: "Upper and lower arch alignment." },
    ] as MockSeaProcedure[],
    events: [
      step("mock-sea-template-step-1", seaTemplateProcedure, seaTemplateProject, "Assessment", "Examine bite and take photos.", "2026-01-05T08:20:00.000Z"),
      step("mock-sea-template-step-2", seaTemplateProcedure, seaTemplateProject, "Impressions", "Take digital scans of both arches.", "2026-01-05T08:21:00.000Z"),
      step("mock-sea-template-step-3", seaTemplateProcedure, seaTemplateProject, "Aligner fitting", "Fit the first aligner set and explain wear.", "2026-01-05T08:22:00.000Z"),
      step("mock-sea-template-step-4", "mock-sea-template-cleaning", "mock-sea-template-hygiene", "Examination", "Check gums and enamel.", "2026-01-06T08:20:00.000Z"),
      step("mock-sea-visit-1", "mock-sea-visits", patient, "First visit", "Initial consultation.", "2026-02-01T09:05:00.000Z", "2026-02-01T09:40:00.000Z"),
      step("mock-sea-event-assessment", seaProcedure, patient, "Assessment", "Examine bite and take photos.", "2026-02-03T09:05:00.000Z", "2026-02-03T09:45:00.000Z"),
      step(seaEvent, seaProcedure, patient, "Impressions", "Take digital scans of both arches.", "2026-02-10T10:00:00.000Z"),
      step("mock-sea-event-fitting", seaProcedure, patient, "Aligner fitting", "Fit the first aligner set and explain wear.", "2026-02-10T10:01:00.000Z"),
    ] as MockSeaEvent[],
    attachments: [
      textNote("mock-sea-note-1", "mock-sea-event-assessment", "Mild crowding on the lower incisors; no pain reported.", "2026-02-03T09:20:00.000Z", clinician),
      { ...tracked("mock-sea-file-1", "2026-02-03T09:30:00.000Z", assistant), eventId: "mock-sea-event-assessment", fileName: "bite-photo.svg", content: mockSeaImage, contentType: "image/svg+xml", size: mockSeaImage.length, kind: 1 },
      textNote("mock-sea-note-2", seaEvent, "Upper arch scanned. Lower arch scan scheduled for the next visit.", "2026-02-10T10:20:00.000Z", clinician),
      textNote("mock-sea-note-4", seaEvent, "The patient asked whether the lower scan can be done on Saturday morning.", "2026-02-10T10:35:00.000Z", assistant),
      textNote("mock-sea-note-5", seaEvent, "Saturday works. Please book 9:30.", "2026-02-10T10:50:00.000Z"),
      textNote("mock-sea-note-3", "mock-sea-visit-1", "Patient prefers morning appointments.", "2026-02-01T09:30:00.000Z"),
    ] as MockSeaAttachment[],
    roles: [
      { ...tracked("mock-sea-role-clinician", "2026-01-01T00:00:00.000Z"), name: "Clinician" },
      { ...tracked("mock-sea-role-assistant", "2026-01-01T00:00:00.000Z"), name: "Assistant" },
    ] as MockSeaRole[],
    eventRoles: [
      { ...tracked("mock-sea-event-role-1", "2026-02-10T10:00:00.000Z"), eventId: seaEvent, roleId: "mock-sea-role-clinician", type: RoleType.Write },
      { ...tracked("mock-sea-event-role-2", "2026-02-10T10:00:00.000Z"), eventId: seaEvent, roleId: "mock-sea-role-assistant", type: RoleType.Read },
    ] as MockSeaEventRole[],
  };
}

const createEmptyMockSeaRecords = (): ReturnType<typeof createMockSeaRecords> => (
  { projects: [], procedures: [], events: [], attachments: [], roles: [], eventRoles: [] }
);

export function createMockStore(scenario: "ready" | "empty") {
  let sequence = 1;
  const store = {
    owners: scenario === "empty" ? [] as MockOwner[] : [{ id: "mock-owner", name: "Mock Practice", description: "Deterministic mock owner." }],
    tenants: scenario === "empty" ? [] as MockTenant[] : [{ id: "mock-tenant", name: "Mock Tenant", appId: "private-practice-owner", ownerId: "mock-owner" }],
    indicators: scenario === "empty" ? [] as MockIndicator[] : [{ id: "mock-indicator", tenantId: "mock-tenant", value: "mock.localhost", method: 1, status: 3, isPrimary: true }],
    users: scenario === "empty" ? [] as MockUser[] : [
      { userId: "mock-user", fullName: "Mock User", userName: "mock.user", email: "mock.user@example.test" },
      { userId: mockFixtureIds.clinician, fullName: "Dr. Sara Amini", userName: "sara.amini", email: "sara.amini@example.test" },
      { userId: mockFixtureIds.assistant, fullName: "Reza Karimi", userName: "reza.karimi", email: "reza.karimi@example.test" },
    ],
    memberships: scenario === "empty" ? [] as Array<MockMembership & { userId: string }>: [
      { userId: "mock-user", tenantId: "mock-tenant", roleSuffix: "Owner" },
      { userId: mockFixtureIds.clinician, tenantId: "mock-tenant", roleSuffix: tenantRoleSuffixes.user },
      { userId: mockFixtureIds.assistant, tenantId: "mock-tenant", roleSuffix: tenantRoleSuffixes.user },
    ],
    patients: scenario === "empty" ? [] as MockPatient[] : [
      { id: mockFixtureIds.patient, tenantId: "mock-tenant", name: "Ada Lovelace", firstName: "Ada", lastName: "Lovelace", identificationCode: "mock-ada-001", phone: "+10000000001", email: "ada@example.test", dateOfBirth: "1990-12-10", createdAt: "2026-01-01T00:00:00.000Z", isDeleted: false },
      { id: "mock-patient-grace", tenantId: "mock-tenant", name: "Grace Hopper", firstName: "Grace", lastName: "Hopper", identificationCode: "mock-grace-002", phone: "+10000000002", email: "grace@example.test", dateOfBirth: "1985-12-09", createdAt: "2026-01-02T00:00:00.000Z", isDeleted: false },
      { id: "mock-patient-katherine", tenantId: "mock-tenant", name: "Katherine Johnson", firstName: "Katherine", lastName: "Johnson", identificationCode: "mock-katherine-003", phone: "+10000000003", email: "katherine@example.test", dateOfBirth: "1988-08-26", createdAt: "2026-01-03T00:00:00.000Z", isDeleted: false },
      { id: "mock-patient-marie", tenantId: "mock-tenant", name: "Marie Curie", firstName: "Marie", lastName: "Curie", identificationCode: "mock-marie-004", phone: "+10000000004", email: "marie@example.test", dateOfBirth: "1992-11-07", createdAt: "2026-01-04T00:00:00.000Z", isDeleted: false },
    ],
    conversations: [] as MockConversation[],
    auditCatalog: mockAuditCatalog,
    auditEvents: scenario === "empty" ? [] as MockAuditEvent[] : createMockAuditEvents(),
    knowledgeAsks: [] as string[],
    appointments: {
      reservations: scenario === "empty" ? [] as Record<string, unknown>[] : [{ id: mockFixtureIds.reservation, createdAt: "2026-01-01T00:00:00.000Z", isDeleted: false, status: 1, shiftId: mockFixtureIds.shift, date: mockFixtureIds.shiftOccurrenceDate, isHold: false }],
      shifts: scenario === "empty" ? [] as Record<string, unknown>[] : [{ id: mockFixtureIds.shift, createdAt: "2026-01-01T00:00:00.000Z", isDeleted: false, status: 1, visibility: 1, dayOfWeek: 2, startTime: "10:30", endTime: "11:00", timeZone: "UTC", totalSeats: 1, serviceProviderId: mockFixtureIds.serviceProvider, serviceId: mockFixtureIds.service }],
      "service-types": scenario === "empty" ? [] as Record<string, unknown>[] : [{ id: mockFixtureIds.serviceType, createdAt: "2026-01-01T00:00:00.000Z", isDeleted: false, name: "Consultation", description: "Mock consultation" }],
      services: scenario === "empty" ? [] as Record<string, unknown>[] : [{ id: mockFixtureIds.service, createdAt: "2026-01-01T00:00:00.000Z", isDeleted: false, name: "Initial consultation", description: "Mock service", serviceTypeId: mockFixtureIds.serviceType, serviceTypeName: "Consultation" }],
      "service-providers": scenario === "empty" ? [] as Record<string, unknown>[] : [{ id: mockFixtureIds.serviceProvider, createdAt: "2026-01-01T00:00:00.000Z", isDeleted: false, name: "Mock clinician", description: "Mock provider", linkedUserIds: ["mock-user"] }],
    },
    sea: scenario === "empty" ? createEmptyMockSeaRecords() : createMockSeaRecords(),
    nextId(prefix: string) { return `${prefix}-${sequence++}`; },
  };
  return store;
}

export type MockStore = ReturnType<typeof createMockStore>;

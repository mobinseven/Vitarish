import type { SecurityAuditSortField } from "./generated/audit/model";

// OpenAPI exposes the C# enum as an integer, so this keeps its wire values owned in one place.
export const auditSortFields = {
  actor: 0,
  occurredAt: 1,
  eventType: 2,
  actorKind: 3,
  sourceIpKind: 4,
  sourceIpAddress: 5,
} as const satisfies Record<string, SecurityAuditSortField>;

export type AuditSortField = typeof auditSortFields[keyof typeof auditSortFields];

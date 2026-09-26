import { auditPlatformSecurityEventsList } from "./api/generated/audit/audit";

export async function recordPlatformAuditAccessAttempt(): Promise<void> {
  try {
    await auditPlatformSecurityEventsList({
      page: 1,
      pageSize: 1,
      sortBy: 0,
      descending: true,
      uncataloguedOnly: false,
    });
  } catch {
    // The server records the denied attempt before returning 403. The page must remain usable if audit storage is unavailable.
  }
}

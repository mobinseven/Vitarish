import { useTenantAuthorization } from "@vitarish/auth";

/**
 * Whether the viewer may plan and manage surgeries.
 *
 * The server enforces owner-only surgery access; this client pre-check only
 * hides or disables the planning actions, never authorizes them.
 */
export function useCanPlanSurgeries(): boolean {
  return useTenantAuthorization("owner").hasAccess;
}

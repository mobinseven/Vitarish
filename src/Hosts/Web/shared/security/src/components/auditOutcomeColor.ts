/** HeroUI semantic status color a security-audit outcome is displayed with. */
export type AuditOutcomeColor = "danger" | "default" | "success" | "warning";

/** Display-only status color for a server-recorded audit outcome; it never decides security state. */
export function auditOutcomeColor(outcome: string | null | undefined): AuditOutcomeColor {
  switch (outcome?.trim().toLowerCase()) {
    case "succeeded":
      return "success";
    case "failed":
    case "denied":
      return "danger";
    case "pending":
    case "started":
      return "warning";
    default:
      return "default";
  }
}

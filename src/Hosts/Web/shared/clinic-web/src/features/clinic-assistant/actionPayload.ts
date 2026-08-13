export type ClinicWorkspaceReviewPayload = Record<string, unknown>;

export function asRecord(value: unknown): ClinicWorkspaceReviewPayload {
  return value && typeof value === "object" && !Array.isArray(value)
    ? (value as ClinicWorkspaceReviewPayload)
    : {};
}

export function asText(value: unknown): string | null {
  return typeof value === "string" && value.trim() ? value : null;
}

export function asNumber(value: unknown): number | null {
  return typeof value === "number" ? value : null;
}

export function asList(value: unknown): unknown[] {
  return Array.isArray(value) ? value : [];
}

export function parseRecord(value: unknown): ClinicWorkspaceReviewPayload {
  if (typeof value !== "string") return asRecord(value);
  try {
    return asRecord(JSON.parse(value));
  } catch {
    return {};
  }
}

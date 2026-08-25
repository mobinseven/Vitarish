import { apiFetch as sharedApiFetch } from "@vitarish/api-client";
import type { OrvalConfig } from "@vitarish/api-client";

export { ApiError, resetAntiforgeryToken } from "@vitarish/api-client";
export type { ProblemDetails } from "@vitarish/api-client";

export function apiFetch<T>(urlOrConfig: string | OrvalConfig, options: RequestInit = {}): Promise<T> {
  return sharedApiFetch<T>(urlOrConfig, options);
}

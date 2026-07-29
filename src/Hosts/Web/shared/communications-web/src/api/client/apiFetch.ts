import { apiFetch as sharedApiFetch } from "@vitarish/web-api-client";
import type { OrvalConfig } from "@vitarish/web-api-client";

export function apiFetch<T>(urlOrConfig: string | OrvalConfig, options: RequestInit = {}): Promise<T> {
  return sharedApiFetch<T>(urlOrConfig, options);
}

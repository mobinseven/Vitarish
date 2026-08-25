import { apiFetch } from "@vitarish/api-client";
import type {
  ContentItem,
  ContentItemInput,
  ContentRevision,
  ContentsClient,
} from "./types";

export function createContentsClient(
  basePath = "/api/contents",
): ContentsClient {
  const itemPath = (id: string) =>
    `${basePath}/items/${encodeURIComponent(id)}`;
  return {
    list: (signal) => apiFetch<ContentItem[]>(`${basePath}/items`, { signal }),
    get: (id, signal) => apiFetch<ContentItem>(itemPath(id), { signal }),
    create: (input) =>
      apiFetch<ContentItem>(`${basePath}/items`, {
        method: "POST",
        body: JSON.stringify(input),
      }),
    update: (id, input) =>
      apiFetch<ContentItem>(itemPath(id), {
        method: "PUT",
        body: JSON.stringify(input),
      }),
    archive: (id) =>
      apiFetch<ContentItem>(`${itemPath(id)}/archive`, { method: "POST" }),
    revisions: (id, signal) =>
      apiFetch<ContentRevision[]>(`${itemPath(id)}/revisions`, { signal }),
  };
}

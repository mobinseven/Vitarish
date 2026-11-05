"use client";

import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import {
  archiveContentEntry,
  createContentEntry,
  createContentSite,
  createContentType,
  getContentEntry,
  listContentEntries,
  listContentRevisions,
  listContentSites,
  listContentTypes,
  publishContentEntry,
  restoreContentRevision,
  saveContentDraft,
  unpublishContentEntry,
  updateContentSite,
  updateContentTypeSchema,
} from "./contentsStudioApi";
import type {
  ContentEntryFilter,
  ContentEntryOutcome,
  ContentEntryRequest,
  ContentSchemaRequest,
  ContentSiteRequest,
  ContentTypeRequest,
  DraftRequest,
  EntryVersionRequest,
  RevisionActionRequest,
} from "./contentsStudioApi";

export const contentStudioKeys = {
  all: ["contentStudio"] as const,
  sites: () => [...contentStudioKeys.all, "sites"] as const,
  types: () => [...contentStudioKeys.all, "types"] as const,
  entries: (filter?: ContentEntryFilter) =>
    filter
      ? ([...contentStudioKeys.all, "entries", filter] as const)
      : ([...contentStudioKeys.all, "entries"] as const),
  entry: (id: string) => [...contentStudioKeys.all, "entry", id] as const,
  revisions: (id: string) =>
    [...contentStudioKeys.entry(id), "revisions"] as const,
};

function invalidateEntryQueries(
  cache: ReturnType<typeof useQueryClient>,
  id: string,
) {
  void cache.invalidateQueries({ queryKey: contentStudioKeys.entry(id) });
  void cache.invalidateQueries({ queryKey: contentStudioKeys.revisions(id) });
  void cache.invalidateQueries({ queryKey: contentStudioKeys.entries() });
}

export function useContentSites() {
  return useQuery({
    queryKey: contentStudioKeys.sites(),
    queryFn: ({ signal }) => listContentSites(signal),
  });
}

export function useContentTypes() {
  return useQuery({
    queryKey: contentStudioKeys.types(),
    queryFn: ({ signal }) => listContentTypes(signal),
  });
}

export function useContentEntries(filter?: ContentEntryFilter) {
  return useQuery({
    queryKey: contentStudioKeys.entries(filter),
    queryFn: ({ signal }) => listContentEntries(filter, signal),
    enabled: Boolean(filter?.siteId),
  });
}

export function useContentEntry(id: string | undefined) {
  return useQuery({
    queryKey: contentStudioKeys.entry(id ?? ""),
    queryFn: ({ signal }) => getContentEntry(id!, signal),
    enabled: Boolean(id),
  });
}

export function useContentRevisions(id: string | undefined) {
  return useQuery({
    queryKey: contentStudioKeys.revisions(id ?? ""),
    queryFn: ({ signal }) => listContentRevisions(id!, signal),
    enabled: Boolean(id),
  });
}

export function useCreateContentSite() {
  const cache = useQueryClient();
  return useMutation({
    mutationFn: (input: ContentSiteRequest) => createContentSite(input),
    onSuccess: () =>
      cache.invalidateQueries({ queryKey: contentStudioKeys.sites() }),
  });
}

export function useUpdateContentSite() {
  const cache = useQueryClient();
  return useMutation({
    mutationFn: ({ id, input }: { id: string; input: ContentSiteRequest }) =>
      updateContentSite(id, input),
    onSuccess: () =>
      cache.invalidateQueries({ queryKey: contentStudioKeys.sites() }),
  });
}

export function useCreateContentType() {
  const cache = useQueryClient();
  return useMutation({
    mutationFn: (input: ContentTypeRequest) => createContentType(input),
    onSuccess: () =>
      cache.invalidateQueries({ queryKey: contentStudioKeys.types() }),
  });
}

export function useUpdateContentTypeSchema() {
  const cache = useQueryClient();
  return useMutation({
    mutationFn: ({ id, input }: { id: string; input: ContentSchemaRequest }) =>
      updateContentTypeSchema(id, input),
    onSuccess: () => {
      void cache.invalidateQueries({ queryKey: contentStudioKeys.types() });
      return cache.invalidateQueries({ queryKey: contentStudioKeys.entries() });
    },
  });
}

export function useCreateContentEntry() {
  const cache = useQueryClient();
  return useMutation({
    mutationFn: (input: ContentEntryRequest) => createContentEntry(input),
    onSuccess: () => cache.invalidateQueries({ queryKey: contentStudioKeys.entries() }),
  });
}

export function useSaveDraft() {
  const cache = useQueryClient();
  return useMutation({
    mutationFn: ({ id, input }: { id: string; input: DraftRequest }) =>
      saveContentDraft(id, input),
    onSuccess: (outcome, variables) =>
      invalidateEntryQueries(cache, outcome.id || variables.id),
  });
}

function useEntryVersionMutation(
  mutation: (id: string, input: EntryVersionRequest) => Promise<ContentEntryOutcome>,
) {
  const cache = useQueryClient();
  return useMutation({
    mutationFn: (variables: { id: string; expectedVersion: number }) =>
      mutation(variables.id, { expectedVersion: variables.expectedVersion }),
    onSuccess: (_outcome, variables) =>
      invalidateEntryQueries(cache, variables.id),
  });
}

export function usePublishEntry() {
  const cache = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      revisionId,
      expectedVersion,
    }: {
      id: string;
      revisionId: string;
      expectedVersion: number;
    }) => publishContentEntry(id, { revisionId, expectedVersion }),
    onSuccess: (_outcome, variables) =>
      invalidateEntryQueries(cache, variables.id),
  });
}

export function useUnpublishEntry() {
  return useEntryVersionMutation(unpublishContentEntry);
}

export function useArchiveEntry() {
  return useEntryVersionMutation(archiveContentEntry);
}

export function useRestoreRevision() {
  const cache = useQueryClient();
  return useMutation({
    mutationFn: ({
      id,
      revisionId,
      expectedVersion,
    }: {
      id: string;
      revisionId: string;
      expectedVersion: number;
    }) => restoreContentRevision(id, { revisionId, expectedVersion }),
    onSuccess: (_outcome, variables) =>
      invalidateEntryQueries(cache, variables.id),
  });
}

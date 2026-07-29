import { useMutation, useQuery, useQueryClient } from "@tanstack/react-query";
import type { ContentItemInput, ContentsClient } from "./types";

export const contentKeys = {
  all: ["contents"] as const,
  list: () => [...contentKeys.all, "list"] as const,
  item: (id: string) => [...contentKeys.all, "item", id] as const,
  revisions: (id: string) => [...contentKeys.item(id), "revisions"] as const,
};
export const useContentItems = (client: ContentsClient) => useQuery({ queryKey: contentKeys.list(), queryFn: ({ signal }) => client.list(signal) });
export const useContentItem = (client: ContentsClient, id: string | undefined) => useQuery({ queryKey: contentKeys.item(id ?? ""), queryFn: ({ signal }) => client.get(id!, signal), enabled: Boolean(id) });
export const useContentRevisions = (client: ContentsClient, id: string | undefined) => useQuery({ queryKey: contentKeys.revisions(id ?? ""), queryFn: ({ signal }) => client.revisions(id!, signal), enabled: Boolean(id) });
export function useSaveContent(client: ContentsClient, id?: string) {
  const cache = useQueryClient();
  return useMutation({ mutationFn: (input: ContentItemInput) => id ? client.update(id, input) : client.create(input), onSuccess: (item) => {
    cache.setQueryData(contentKeys.item(item.id), item);
    void cache.invalidateQueries({ queryKey: contentKeys.list() });
    void cache.invalidateQueries({ queryKey: contentKeys.revisions(item.id) });
  }});
}
export function useArchiveContent(client: ContentsClient) {
  const cache = useQueryClient();
  return useMutation({ mutationFn: (id: string) => client.archive(id), onSuccess: (item) => {
    cache.setQueryData(contentKeys.item(item.id), item);
    void cache.invalidateQueries({ queryKey: contentKeys.list() });
    void cache.invalidateQueries({ queryKey: contentKeys.revisions(item.id) });
  }});
}

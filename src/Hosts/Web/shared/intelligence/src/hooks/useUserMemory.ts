"use client";

import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  getIntelligenceGetUserMemoryQueryKey,
  intelligenceForgetUserMemory,
  intelligenceForgetUserMemoryItem,
  intelligenceUpdateUserMemoryItem,
  useIntelligenceGetUserMemory,
} from "../api/generated/intelligence/intelligence";

/**
 * What Vitarish remembers about the signed-in person, with the changes only they can make. Every
 * change refreshes the list from the server; nothing is updated optimistically.
 */
export function useUserMemory({ enabled = true }: { enabled?: boolean } = {}) {
  const queryClient = useQueryClient();
  const memory = useIntelligenceGetUserMemory({ query: { enabled, retry: false } });
  const refresh = () => queryClient.invalidateQueries({ queryKey: getIntelligenceGetUserMemoryQueryKey() });

  const update = useMutation({
    mutationFn: ({ itemId, text }: { itemId: string; text: string }) => intelligenceUpdateUserMemoryItem(itemId, { text }),
    onSuccess: refresh,
  });
  const forget = useMutation({
    mutationFn: (itemId: string) => intelligenceForgetUserMemoryItem(itemId),
    onSuccess: refresh,
  });
  const forgetAll = useMutation({
    mutationFn: () => intelligenceForgetUserMemory(),
    onSuccess: refresh,
  });

  return { memory, update, forget, forgetAll };
}

import { useQuery, type QueryKey } from "@tanstack/react-query";
import type { KnowledgeQaClient } from "../types";

export const knowledgeUserQaKeys = {
  all: ["knowledge", "normal-user", "qa"] as const,
  workspace: () => [...knowledgeUserQaKeys.all, "workspace"] as const,
};

interface UseKnowledgeUserWorkspaceOptions {
  client: KnowledgeQaClient;
  queryKey?: QueryKey;
}

export function useKnowledgeUserWorkspace({
  client,
  queryKey = knowledgeUserQaKeys.workspace(),
}: UseKnowledgeUserWorkspaceOptions) {
  return useQuery({
    queryKey,
    queryFn: () => client.getWorkspace(),
  });
}

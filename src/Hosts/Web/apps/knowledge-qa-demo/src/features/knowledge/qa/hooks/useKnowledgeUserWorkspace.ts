import { useQuery } from "@tanstack/react-query";
import { getKnowledgeUserWorkspace } from "../api/knowledgeUserApi";

export const knowledgeUserQaKeys = {
  all: ["knowledge", "normal-user", "qa"] as const,
  workspace: () => [...knowledgeUserQaKeys.all, "workspace"] as const,
};

export function useKnowledgeUserWorkspace() {
  return useQuery({
    queryKey: knowledgeUserQaKeys.workspace(),
    queryFn: getKnowledgeUserWorkspace,
  });
}

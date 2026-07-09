import { apiFetch } from "@/api/client/apiFetch";
import type {
  KnowledgeAskRequest,
  KnowledgeAnswerModel,
  KnowledgeUserWorkspaceModel,
  KnowledgeUserWorkspace,
} from "../types";

const knowledgeBase = "/api/modules/knowledge";

export async function getKnowledgeUserWorkspace(): Promise<KnowledgeUserWorkspace> {
  const workspace = await apiFetch<KnowledgeUserWorkspaceModel>(`${knowledgeBase}/workspace`);

  return {
    corpora: [...(workspace?.corpora ?? [])].sort((left, right) =>
      left.name.localeCompare(right.name)
    ),
  };
}

export async function askKnowledgeCorpus(
  request: KnowledgeAskRequest
): Promise<KnowledgeAnswerModel> {
  return apiFetch<KnowledgeAnswerModel>(`${knowledgeBase}/ask`, {
    method: "POST",
    body: JSON.stringify({
      corpusId: request.corpusId,
      question: request.question,
      topK: request.topK ?? 8,
      saveTrace: request.saveTrace ?? false,
      traceKind: request.traceKind ?? "normal-user-ui",
    }),
  });
}

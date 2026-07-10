import {
  type KnowledgeAnswerModel,
  type KnowledgeAskRequest,
  type KnowledgeQaClient,
  type KnowledgeUserWorkspace,
  type KnowledgeUserWorkspaceModel,
} from "@vitarish/knowledge-web";
import { ApiError, apiFetch } from "@/api/client/apiFetch";

const knowledgeBase = "/api/modules/knowledge";

export const knowledgeQaClient: KnowledgeQaClient = {
  getWorkspace,
  askCorpus,
};

async function getWorkspace(): Promise<KnowledgeUserWorkspace> {
  const workspace = await apiFetch<KnowledgeUserWorkspaceModel>(`${knowledgeBase}/workspace`);

  return {
    corpora: [...(workspace?.corpora ?? [])].sort((left, right) =>
      left.name.localeCompare(right.name)
    ),
  };
}

async function askCorpus(request: KnowledgeAskRequest): Promise<KnowledgeAnswerModel> {
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

export function getKnowledgeQaErrorMessage(error: unknown, fallbackMessage: string): string {
  if (error instanceof ApiError) {
    return error.message;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return fallbackMessage;
}

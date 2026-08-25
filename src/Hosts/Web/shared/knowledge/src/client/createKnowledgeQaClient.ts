import type { ApiError } from "@vitarish/api-client";
import type {
  KnowledgeAnswerModel,
  KnowledgeAskRequest,
  KnowledgeQaClient,
  KnowledgeUserWorkspace,
  KnowledgeUserWorkspaceModel,
} from "../types";

type ApiFetch = <T>(url: string, options?: RequestInit) => Promise<T>;

const knowledgeBase = "/api/modules/knowledge";

export function createKnowledgeQaClient(fetchFn: ApiFetch): KnowledgeQaClient {
  return {
    getWorkspace: () => getWorkspace(fetchFn),
    askCorpus: (request) => askCorpus(fetchFn, request),
  };
}

async function getWorkspace(fetchFn: ApiFetch): Promise<KnowledgeUserWorkspace> {
  const workspace = await fetchFn<KnowledgeUserWorkspaceModel>(`${knowledgeBase}/workspace`);

  return {
    corpora: [...(workspace?.corpora ?? [])].sort((left, right) =>
      left.name.localeCompare(right.name)
    ),
  };
}

async function askCorpus(fetchFn: ApiFetch, request: KnowledgeAskRequest): Promise<KnowledgeAnswerModel> {
  return fetchFn<KnowledgeAnswerModel>(`${knowledgeBase}/ask`, {
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
  if (typeof error === "object" && error !== null && "message" in error) {
    return String((error as { message: unknown }).message);
  }

  if (error instanceof Error) {
    return error.message;
  }

  return fallbackMessage;
}

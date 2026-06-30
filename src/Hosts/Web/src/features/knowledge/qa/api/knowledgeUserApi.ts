import { apiFetch } from "@/api/client/apiFetch";
import type {
  KnowledgeAskRequest,
  KnowledgeAnswerModel,
  KnowledgeOwnerCollectionModel,
  KnowledgeOwnerCorpusCollectionItem,
  KnowledgeUserCorpus,
  KnowledgeUserWorkspace,
} from "../types";

const knowledgeBase = "/api/modules/knowledge";

export async function getKnowledgeUserWorkspace(): Promise<KnowledgeUserWorkspace> {
  const ownerWorkspace = await apiFetch<KnowledgeOwnerCollectionModel>(
    `${knowledgeBase}/owner/workspace`,
    { method: "POST", body: JSON.stringify({}) }
  );

  return {
    corpora: mapUserCorpora(ownerWorkspace?.corpora ?? []),
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

function mapUserCorpora(
  corpora: KnowledgeOwnerCorpusCollectionItem[]
): KnowledgeUserCorpus[] {
  return corpora
    .filter((corpus) => corpus.isActive !== false)
    .map((corpus) => {
      const sources = corpus.sources ?? [];
      return {
        corpusId: corpus.corpusId,
        name: corpus.corpusName,
        description: corpus.corpusDescription,
        isActive: corpus.isActive !== false,
        sourceCount: sources.length,
        chunkCount: sources.reduce(
          (total, source) =>
            total +
            (source.files ?? []).reduce(
              (fileTotal, file) => fileTotal + (file.chunkCount ?? 0),
              0
            ),
          0
        ),
      };
    })
    .sort((left, right) => left.name.localeCompare(right.name));
}

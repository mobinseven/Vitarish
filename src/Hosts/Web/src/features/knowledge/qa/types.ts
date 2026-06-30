export type KnowledgeUserScopeKind = "all" | "corpus";

export interface KnowledgeUserCorpus {
  corpusId: string;
  name: string;
  description?: string | null;
  isActive: boolean;
  sourceCount: number;
  chunkCount: number;
}

export interface KnowledgeCitation {
  chunkId: string;
  sourceId: string;
  documentId: string;
  title?: string | null;
  quote?: string | null;
}

export interface KnowledgeRetrievalHit {
  chunkId: string;
  sourceId: string;
  documentId: string;
  text: string;
  title?: string | null;
  score: number;
}

export interface KnowledgeAnswerModel {
  answer: string;
  isAnswered: boolean;
  citations: KnowledgeCitation[];
  missingInformation?: string | null;
  retrievedContext: KnowledgeRetrievalHit[];
}

export interface KnowledgeCorpusAnswerResult {
  corpus: KnowledgeUserCorpus;
  question: string;
  answer?: KnowledgeAnswerModel;
  errorMessage?: string;
}

export interface KnowledgeAskRequest {
  corpusId: string;
  question: string;
  topK?: number;
  saveTrace?: boolean;
  traceKind?: string | null;
}

export interface KnowledgeAskAcrossCorporaRequest {
  corpora: KnowledgeUserCorpus[];
  question: string;
  topK?: number;
}

export interface KnowledgeUserWorkspace {
  corpora: KnowledgeUserCorpus[];
}

export interface KnowledgeQuestionScope {
  kind: KnowledgeUserScopeKind;
  corpusId?: string;
}

export interface KnowledgeOwnerCollectionModel {
  corpora?: KnowledgeOwnerCorpusCollectionItem[] | null;
  selectedCorpusId?: string | null;
  focusedSourceId?: string | null;
}

export interface KnowledgeOwnerCorpusCollectionItem {
  corpusId: string;
  corpusName: string;
  corpusDescription?: string | null;
  isActive?: boolean | null;
  sources?: KnowledgeOwnerSourceCollectionItem[] | null;
}

export interface KnowledgeOwnerSourceCollectionItem {
  sourceId: string;
  sourceName: string;
  sourceType: string;
  importedAt?: string | null;
  originalLocation?: string | null;
  files?: KnowledgeOwnerSourceFileCollectionItem[] | null;
}

export interface KnowledgeOwnerSourceFileCollectionItem {
  fileId: string;
  fileName: string;
  contentType?: string | null;
  size?: number | null;
  chunkCount?: number | null;
  indexedAt?: string | null;
}

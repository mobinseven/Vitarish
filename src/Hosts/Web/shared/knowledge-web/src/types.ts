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

export interface KnowledgeUserWorkspaceModel {
  corpora?: KnowledgeUserCorpus[] | null;
}

export interface KnowledgeQaClient {
  getWorkspace(): Promise<KnowledgeUserWorkspace>;
  askCorpus(request: KnowledgeAskRequest): Promise<KnowledgeAnswerModel>;
}

export type KnowledgeQaErrorMessageResolver = (error: unknown) => string;

export interface KnowledgeQaLabels {
  allCorpora: string;
  allCorporaDescription: string;
  allCorpusAnswer: string;
  answeredStatus: string;
  assistantAvatarAlt: string;
  assistantAvatarFallback: string;
  askAction: string;
  askAgainAction: string;
  askFromCorpusAction: string;
  checkingScope: (scope: string) => string;
  chunkCount: (count: number) => string;
  citations: string;
  composerFooter: string;
  contextSection: string;
  emptyDescription: string;
  emptyTitle: string;
  loadCorporaFailed: string;
  loadingCorpora: string;
  missingInformationPrefix: string;
  needsContextStatus: string;
  newQuestionAction: string;
  noCorpora: string;
  noSourcesForQuestion: string;
  questionLabel: string;
  questionPlaceholder: string;
  questionRequired: string;
  railEyebrow: string;
  railTitle: string;
  referencedChunk: string;
  retrievedContext: (count: number) => string;
  scopeLabel: (scope: string) => string;
  score: (score: string) => string;
  searchingContext: string;
  selectedCorpus: string;
  selectedCorpusAnswer: string;
  sourceCount: (count: number) => string;
  thinking: string;
  title: string;
  unavailableStatus: string;
}

import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { render, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { KnowledgeUserQaSurface } from "../index";
import type { KnowledgeQaClient, KnowledgeQaLabels } from "../index";

const labels: KnowledgeQaLabels = {
  allCorpora: "All corpora",
  allCorporaDescription: "Ask across every corpus",
  allCorpusAnswer: "All-corpus answer",
  answeredStatus: "Answered",
  assistantAvatarAlt: "Assistant",
  assistantAvatarFallback: "AI",
  askAction: "Ask",
  askAgainAction: "Ask again",
  askFromCorpusAction: "Ask from this corpus",
  checkingScope: (scope) => `Checking ${scope}`,
  chunkCount: (count) => `${count} chunks`,
  citations: "Citations",
  composerFooter: "Grounded answers",
  contextSection: "Context section",
  emptyDescription: "Ask a question to start.",
  emptyTitle: "Choose a corpus",
  loadCorporaFailed: "Could not load corpora",
  loadingCorpora: "Loading corpora",
  missingInformationPrefix: "Missing information:",
  needsContextStatus: "Needs more context",
  newQuestionAction: "New question",
  noCorpora: "No corpora",
  noSourcesForQuestion: "No sources",
  questionLabel: "Question",
  questionPlaceholder: "Ask a question",
  questionRequired: "Question is required",
  railEyebrow: "Corpora",
  railTitle: "Knowledge sources",
  referencedChunk: "Referenced chunk",
  retrievedContext: (count) => `Retrieved context ${count}`,
  scopeLabel: (scope) => `Scope: ${scope}`,
  score: (score) => `Score ${score}`,
  searchingContext: "Searching",
  selectedCorpus: "Selected corpus",
  selectedCorpusAnswer: "Selected-corpus answer",
  sourceCount: (count) => `${count} sources`,
  thinking: "Thinking",
  title: "Knowledge Q&A",
  unavailableStatus: "Unavailable",
};

describe("@vitarish/knowledge-web", () => {
  it("renders corpora and caller-provided labels through the shared surface", async () => {
    const client: KnowledgeQaClient = {
      getWorkspace: vi.fn(async () => ({
        corpora: [
          {
            corpusId: "corpus-1",
            name: "Clinical guide",
            description: "Approved answers",
            isActive: true,
            sourceCount: 2,
            chunkCount: 12,
          },
        ],
      })),
      askCorpus: vi.fn(),
    };

    render(
      <QueryClientProvider client={new QueryClient()}>
        <KnowledgeUserQaSurface client={client} labels={labels} />
      </QueryClientProvider>
    );

    expect(screen.getByText("Choose a corpus")).toBeInTheDocument();
    expect(screen.getByLabelText("Question")).toBeInTheDocument();
    await waitFor(() => {
      expect(screen.getByLabelText("Knowledge sources")).toHaveTextContent("All corpora");
      expect(screen.getByText("Clinical guide")).toBeInTheDocument();
    });
  });
});

"use client";

import { Button, Card } from "@heroui/react";
import type { KnowledgeCorpusAnswerResult } from "../types";

interface KnowledgeAnswerCardProps {
  result: KnowledgeCorpusAnswerResult;
  isSingleCorpusMode: boolean;
  onAskAgain: () => void;
  onContinueInCorpus: (corpusId: string) => void;
}

export function KnowledgeAnswerCard({
  result,
  isSingleCorpusMode,
  onAskAgain,
  onContinueInCorpus,
}: KnowledgeAnswerCardProps) {
  const { corpus, answer, errorMessage } = result;
  const citations = answer?.citations ?? [];
  const retrievedContext = answer?.retrievedContext ?? [];

  return (
    <Card className="glass-card">
      <Card.Header>
        <div className="flex w-full flex-col gap-2 md:flex-row md:items-start md:justify-between">
          <div>
            <p className="text-xs uppercase tracking-[0.25em] text-muted">Corpus</p>
            <h2 className="text-xl font-semibold">{corpus.name}</h2>
            {corpus.description && (
              <p className="mt-1 text-sm text-muted">{corpus.description}</p>
            )}
          </div>
          <AnswerStatus isAnswered={answer?.isAnswered} hasError={!!errorMessage} />
        </div>
      </Card.Header>

      <Card.Content className="flex flex-col gap-5">
        {errorMessage && (
          <div className="rounded-2xl border border-danger-200 bg-danger-50 p-4 text-sm text-danger-700">
            {errorMessage}
          </div>
        )}

        {!errorMessage && answer && (
          <>
            <section className="rounded-2xl bg-default-50 p-4">
              <p className="text-sm leading-7 whitespace-pre-wrap">
                {answer.isAnswered ? answer.answer : answer.missingInformation ?? answer.answer}
              </p>
            </section>

            {answer.missingInformation && answer.isAnswered && (
              <section className="rounded-2xl border border-warning-200 bg-warning-50 p-4 text-sm text-warning-700">
                <span className="font-medium">Missing information: </span>
                {answer.missingInformation}
              </section>
            )}

            {citations.length > 0 && (
              <section className="flex flex-col gap-2">
                <h3 className="text-sm font-semibold">Citations</h3>
                <div className="grid gap-2">
                  {citations.map((citation) => (
                    <div
                      key={`${citation.chunkId}-${citation.sourceId}`}
                      className="rounded-xl border border-default-200 p-3 text-sm"
                    >
                      <div className="font-medium">
                        {citation.title ?? "Referenced chunk"}
                      </div>
                      {citation.quote && (
                        <p className="mt-1 text-muted">“{citation.quote}”</p>
                      )}
                    </div>
                  ))}
                </div>
              </section>
            )}

            {retrievedContext.length > 0 && (
              <details className="rounded-2xl border border-default-200 p-4">
                <summary className="cursor-pointer text-sm font-semibold">
                  Retrieved context ({retrievedContext.length})
                </summary>
                <div className="mt-3 grid gap-3">
                  {retrievedContext.map((hit) => (
                    <article key={hit.chunkId} className="rounded-xl bg-default-50 p-3">
                      <div className="flex items-center justify-between gap-3 text-xs text-muted">
                        <span>{hit.title ?? "Context section"}</span>
                        <span>Score {hit.score.toFixed(2)}</span>
                      </div>
                      <p className="mt-2 line-clamp-4 text-sm">{hit.text}</p>
                    </article>
                  ))}
                </div>
              </details>
            )}
          </>
        )}

        <div className="flex flex-wrap gap-2">
          <Button type="button" onPress={onAskAgain}>
            Ask again
          </Button>
          {!isSingleCorpusMode && (
            <Button
              type="button"
              onPress={() => onContinueInCorpus(corpus.corpusId)}
              className="bg-transparent"
            >
              Ask from this corpus
            </Button>
          )}
        </div>
      </Card.Content>
    </Card>
  );
}

function AnswerStatus({
  isAnswered,
  hasError,
}: {
  isAnswered?: boolean;
  hasError: boolean;
}) {
  const label = hasError ? "Unavailable" : isAnswered ? "Answered" : "Needs more context";
  const tone = hasError
    ? "bg-danger-100 text-danger-700"
    : isAnswered
      ? "bg-success-100 text-success-700"
      : "bg-warning-100 text-warning-700";

  return (
    <span className={`rounded-full px-3 py-1 text-xs font-medium ${tone}`}>
      {label}
    </span>
  );
}

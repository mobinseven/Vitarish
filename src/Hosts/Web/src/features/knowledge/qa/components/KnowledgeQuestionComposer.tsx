"use client";

import { Button, Card } from "@heroui/react";
import { SurfaceCard } from "@/design-system";
import { KnowledgePromptInput } from "./chat";

interface KnowledgeQuestionComposerProps {
  value: string;
  scopeLabel: string;
  isPending: boolean;
  hasAnswer: boolean;
  onChange: (value: string) => void;
  onSubmit: (question: string) => void;
  onNewQuestion: () => void;
}

export function KnowledgeQuestionComposer({
  value,
  scopeLabel,
  isPending,
  hasAnswer,
  onChange,
  onSubmit,
  onNewQuestion,
}: KnowledgeQuestionComposerProps) {
  return (
    <SurfaceCard>
      <Card.Content>
        <div className="flex flex-col gap-4">
          <div className="flex flex-col gap-1">
            <p className="text-xs uppercase tracking-[0.25em] text-muted">Ask</p>
            <h1 className="text-3xl font-semibold">Ask anything</h1>
          </div>

          <KnowledgePromptInput
            value={value}
            label="Question"
            placeholder="Ask from your selected corpus or all corpora"
            isPending={isPending}
            submitLabel="Ask"
            helperText={`Scope: ${scopeLabel}`}
            onChange={onChange}
            onSubmit={onSubmit}
            actions={
              hasAnswer ? (
                <Button type="button" onPress={onNewQuestion} className="bg-transparent">
                  New question
                </Button>
              ) : null
            }
          />
        </div>
      </Card.Content>
    </SurfaceCard>
  );
}

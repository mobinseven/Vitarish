"use client";

import type { FormEvent } from "react";
import { Button, Card, Input, Label, TextField } from "@heroui/react";

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
  const handleSubmit = (event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    onSubmit(value);
  };

  return (
    <Card className="glass-card">
      <Card.Content>
        <form onSubmit={handleSubmit} className="flex flex-col gap-4">
          <div className="flex flex-col gap-1">
            <p className="text-xs uppercase tracking-[0.25em] text-muted">Ask</p>
            <h1 className="text-3xl font-semibold">Ask anything</h1>
            <p className="text-sm text-muted">
              Scope: <span className="font-medium text-foreground">{scopeLabel}</span>
            </p>
          </div>

          <div className="flex flex-col gap-3 md:flex-row md:items-end">
            <TextField
              name="knowledgeQuestion"
              isRequired
              isDisabled={isPending}
              className="flex-1"
            >
              <Label>Question</Label>
              <Input
                placeholder="Ask from your selected corpus or all corpora"
                value={value}
                onChange={(event) => onChange(event.target.value)}
                autoComplete="off"
              />
            </TextField>

            <Button type="submit" isPending={isPending} className="md:min-w-32">
              Ask
            </Button>
          </div>

          {hasAnswer && (
            <div className="flex flex-wrap gap-2">
              <Button type="button" onPress={onNewQuestion} className="bg-transparent">
                New question
              </Button>
            </div>
          )}
        </form>
      </Card.Content>
    </Card>
  );
}

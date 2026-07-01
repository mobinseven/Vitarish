"use client";

import { Button, Card } from "@heroui/react";
import { useLocalizedText } from "@/localization";
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
  const { t } = useLocalizedText();

  return (
    <Card>
      <Card.Content>
        <div className="flex flex-col gap-4">
          <div className="flex flex-col gap-1">
            <p className="text-xs uppercase tracking-[0.25em] text-default-500">{t("knowledge.qa.composerEyebrow")}</p>
            <h1 className="text-3xl font-semibold">{t("knowledge.qa.composerTitle")}</h1>
          </div>

          <KnowledgePromptInput
            value={value}
            label={t("knowledge.qa.questionLabel")}
            placeholder={t("knowledge.qa.questionPlaceholder")}
            isPending={isPending}
            submitLabel={t("knowledge.qa.askAction")}
            helperText={t("knowledge.qa.scopeLabel", { scope: scopeLabel })}
            onChange={onChange}
            onSubmit={onSubmit}
            actions={
              hasAnswer ? (
                <Button type="button" variant="ghost" onPress={onNewQuestion}>
                  {t("knowledge.qa.newQuestionAction")}
                </Button>
              ) : null
            }
          />
        </div>
      </Card.Content>
    </Card>
  );
}

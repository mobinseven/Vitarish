"use client";

import { useState } from "react";
import { Button, Chip, Description, Label, Tooltip } from "@heroui/react";
import { CheckboxButtonGroup, RadioButtonGroup } from "@heroui-pro/react";
import { useLocalizedText } from "@vitarish/localization";
import { FaIcon } from "@vitarish/ui";
import type { InteractiveQuestion } from "../interaction/interactiveReply";

export interface InteractiveOptionPickerProps {
  question: InteractiveQuestion;
  /** The keys already chosen; once set, the question shows them and no longer accepts answers. */
  answered?: readonly string[];
  isDisabled?: boolean;
  onAnswer: (keys: string[]) => void;
}

/**
 * Lets a person answer an agent's question by tapping. A single-choice question answers on the
 * first tap; a multiple-choice one collects up to its limit and answers with a send button.
 */
export function InteractiveOptionPicker({ question, answered = [], isDisabled = false, onAnswer }: InteractiveOptionPickerProps) {
  const { t } = useLocalizedText();
  const [selected, setSelected] = useState<string[]>([]);
  const isAnswered = answered.length > 0;
  const name = `interactive-${question.options.map((option) => option.key).join("-")}`;

  // An answered question stays readable: its prompt and the chosen options, no longer a control.
  if (isAnswered) {
    const chosen = question.options.filter((option) => answered.includes(option.key));
    return (
      <div className="flex flex-col gap-2">
        <p className="text-muted">{question.prompt}</p>
        <ul aria-label={question.prompt} className="flex flex-wrap gap-2">
          {chosen.map((option) => (
            <li key={option.key}>
              <Chip color="accent" variant="soft">{option.label}</Chip>
            </li>
          ))}
        </ul>
      </div>
    );
  }

  if (question.maxSelections <= 1) {
    return (
      <RadioButtonGroup
        isDisabled={isDisabled}
        name={name}
        value={null}
        variant="secondary"
        onChange={(value) => onAnswer([String(value)])}
      >
        <Label>{question.prompt}</Label>
        {question.options.map((option) => (
          <RadioButtonGroup.Item key={option.key} value={option.key}>
            <RadioButtonGroup.Indicator />
            <RadioButtonGroup.ItemContent>
              <Label>{option.label}</Label>
            </RadioButtonGroup.ItemContent>
          </RadioButtonGroup.Item>
        ))}
      </RadioButtonGroup>
    );
  }

  const value = selected;
  return (
    <div className="flex flex-col gap-3">
      <CheckboxButtonGroup
        isDisabled={isDisabled}
        name={name}
        value={value}
        variant="secondary"
        onChange={(next) => setSelected(next.slice(0, question.maxSelections))}
      >
        <Label>{question.prompt}</Label>
        <Description>{t("intelligence.interactive.limit", { count: question.maxSelections })}</Description>
        {question.options.map((option) => (
          <CheckboxButtonGroup.Item
            key={option.key}
            isDisabled={!value.includes(option.key) && value.length >= question.maxSelections}
            value={option.key}
          >
            <CheckboxButtonGroup.Indicator />
            <CheckboxButtonGroup.ItemContent>
              <Label>{option.label}</Label>
            </CheckboxButtonGroup.ItemContent>
          </CheckboxButtonGroup.Item>
        ))}
      </CheckboxButtonGroup>
      <Tooltip>
        <Button className="w-fit" isDisabled={isDisabled || selected.length === 0} onPress={() => onAnswer(selected)}>
          <FaIcon aria-hidden="true" icon="paper-plane" />
          {t("intelligence.interactive.submit")}
        </Button>
        <Tooltip.Content>{t("intelligence.interactive.submitTooltip")}</Tooltip.Content>
      </Tooltip>
    </div>
  );
}

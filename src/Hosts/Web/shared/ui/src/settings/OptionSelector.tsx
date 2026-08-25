"use client";

import { Radio, RadioGroup } from "@heroui/react";

export interface OptionSelectorOption<T extends string> {
  value: T;
  label: string;
}

export interface OptionSelectorProps<T extends string> {
  title: string;
  options: readonly OptionSelectorOption<T>[];
  selected: T;
  onSelect: (value: T) => void;
}

export function OptionSelector<T extends string>({
  onSelect,
  options,
  selected,
  title,
}: OptionSelectorProps<T>) {
  return (
    <section className="flex w-full min-w-0 flex-col gap-4">
      <h1 className="whitespace-normal break-words text-2xl font-bold">
        {title}
      </h1>
      <RadioGroup
        aria-label={title}
        value={selected}
        onChange={(value) => onSelect(value as T)}
        className="flex min-w-0 flex-col gap-2"
      >
        {options.map((option) => (
          <Radio key={option.value} value={option.value}>
            <Radio.Content className="whitespace-normal break-words">
              <Radio.Control>
                <Radio.Indicator />
              </Radio.Control>
              {option.label}
            </Radio.Content>
          </Radio>
        ))}
      </RadioGroup>
    </section>
  );
}

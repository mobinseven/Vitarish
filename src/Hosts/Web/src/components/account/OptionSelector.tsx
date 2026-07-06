"use client";

import { Radio, RadioGroup } from "@heroui/react";

interface Option<T extends string> {
  value: T;
  label: string;
}

interface OptionSelectorProps<T extends string> {
  title: string;
  options: Option<T>[];
  selected: T;
  onSelect: (value: T) => void;
}

export function OptionSelector<T extends string>({
  title,
  options,
  selected,
  onSelect,
}: OptionSelectorProps<T>) {
  return (
    <section className="flex w-full min-w-0 flex-col gap-4">
      <h1 className="whitespace-normal break-words text-2xl font-bold">{title}</h1>
      <RadioGroup
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

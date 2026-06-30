"use client";

import { Card, Button } from "@heroui/react";
import { SurfaceCard } from "@/design-system";

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
    <SurfaceCard>
      <Card.Header>
        <h1 className="text-2xl font-bold">{title}</h1>
      </Card.Header>
      <Card.Content>
        <div className="flex flex-col gap-2">
          {options.map((option) => (
            <Button
              key={option.value}
              variant={selected === option.value ? "primary" : "tertiary"}
              onPress={() => onSelect(option.value)}
              className="w-full justify-start"
            >
              {option.label}
            </Button>
          ))}
        </div>
      </Card.Content>
    </SurfaceCard>
  );
}

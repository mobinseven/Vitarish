"use client";

import { Card, CardBody, CardHeader, Button } from "@heroui/react";

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
    <Card>
      <CardHeader>
        <h1 className="text-2xl font-bold">{title}</h1>
      </CardHeader>
      <CardBody>
        <div className="flex flex-col gap-2">
          {options.map((option) => (
            <Button
              key={option.value}
              variant={selected === option.value ? "solid" : "flat"}
              color={selected === option.value ? "primary" : "default"}
              onPress={() => onSelect(option.value)}
              className="w-full justify-start"
            >
              {option.label}
            </Button>
          ))}
        </div>
      </CardBody>
    </Card>
  );
}

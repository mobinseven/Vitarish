"use client";

import { useCallback, useMemo } from "react";
import {
  Calendar,
  DateField,
  DatePicker,
  Description,
  FieldError,
  Label,
} from "@heroui/react";
import type { DateValue } from "@internationalized/date";
import { parseIsoDateValue, dateValueToIsoDate } from "@/localization/dateValue";

export interface LocalizedDateFieldProps {
  label: string;
  value: string;
  onChange: (value: string) => void;
  error?: string;
  description?: string;
  name?: string;
  isRequired?: boolean;
  isDisabled?: boolean;
  minValue?: string;
  maxValue?: string;
}

export function LocalizedDateField({
  label,
  value,
  onChange,
  error,
  description,
  name,
  isRequired,
  isDisabled,
  minValue,
  maxValue,
}: LocalizedDateFieldProps) {
  const parsedValue = useMemo(() => parseIsoDateValue(value), [value]);
  const parsedMinValue = useMemo(() => parseIsoDateValue(minValue), [minValue]);
  const parsedMaxValue = useMemo(() => parseIsoDateValue(maxValue), [maxValue]);

  const handleChange = useCallback(
    (dateValue: DateValue | null) => {
      onChange(dateValueToIsoDate(dateValue));
    },
    [onChange]
  );

  return (
    <DatePicker
      className="w-full"
      value={parsedValue}
      onChange={handleChange}
      isInvalid={!!error}
      isRequired={isRequired}
      isDisabled={isDisabled}
      minValue={parsedMinValue ?? undefined}
      maxValue={parsedMaxValue ?? undefined}
      name={name}
    >
      <Label>{label}</Label>
      <DateField.Group fullWidth>
        <DateField.Input>
          {(segment) => <DateField.Segment segment={segment} />}
        </DateField.Input>
        <DateField.Suffix>
          <DatePicker.Trigger>
            <DatePicker.TriggerIndicator />
          </DatePicker.Trigger>
        </DateField.Suffix>
      </DateField.Group>
      {description && !error && <Description>{description}</Description>}
      {error && <FieldError>{error}</FieldError>}
      <DatePicker.Popover className="localized-date-field__popover !min-w-0 w-max max-w-[calc(100vw-2rem)] p-2">
        <Calendar aria-label={label} className="w-max">
          <Calendar.Header>
            <Calendar.YearPickerTrigger>
              <Calendar.YearPickerTriggerHeading />
              <Calendar.YearPickerTriggerIndicator />
            </Calendar.YearPickerTrigger>
            <Calendar.NavButton slot="previous" />
            <Calendar.NavButton slot="next" />
          </Calendar.Header>
          <Calendar.Grid>
            <Calendar.GridHeader>
              {(day) => <Calendar.HeaderCell>{day}</Calendar.HeaderCell>}
            </Calendar.GridHeader>
            <Calendar.GridBody>{(date) => <Calendar.Cell date={date} />}</Calendar.GridBody>
          </Calendar.Grid>
          <Calendar.YearPickerGrid>
            <Calendar.YearPickerGridBody>
              {({ year }) => <Calendar.YearPickerCell year={year} />}
            </Calendar.YearPickerGridBody>
          </Calendar.YearPickerGrid>
        </Calendar>
      </DatePicker.Popover>
    </DatePicker>
  );
}

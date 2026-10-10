"use client";

import { Button, Calendar, DateField, DatePicker, Label, Tooltip } from "@heroui/react";
import { dateValueToIsoDate, parseIsoDateValue, toLocalizedDateValue, type SupportedLocale } from "@vitarish/localization";
import { FaIcon } from "@vitarish/ui";

/** Localized-calendar day picker for an audit date filter; the value is an ISO `yyyy-mm-dd` date or empty. */
export function AuditDateFilter({
  label,
  value,
  onChange,
  onClear,
  clearLabel,
  locale,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  onClear: () => void;
  clearLabel: string;
  locale: SupportedLocale;
}) {
  const dateValue = toLocalizedDateValue(parseIsoDateValue(value), locale);

  return (
    <div className="flex min-w-0 items-end gap-2">
      <DatePicker
        className="min-w-0 flex-1"
        granularity="day"
        value={dateValue}
        onChange={(nextValue) => onChange(dateValueToIsoDate(nextValue))}
      >
        <Label>{label}</Label>
        <DateField.Group fullWidth variant="secondary">
          <DateField.Input>{(segment) => <DateField.Segment segment={segment} />}</DateField.Input>
          <DateField.Suffix>
            <DatePicker.Trigger aria-label={label}>
              <DatePicker.TriggerIndicator />
            </DatePicker.Trigger>
          </DateField.Suffix>
        </DateField.Group>
        <DatePicker.Popover className="w-[24rem] max-w-[calc(100vw-2rem)]">
          <Calendar aria-label={label} className="mx-auto w-full max-w-none">
            <Calendar.Header>
              <Calendar.Heading />
              <Calendar.NavButton slot="previous" />
              <Calendar.NavButton slot="next" />
            </Calendar.Header>
            <Calendar.Grid>
              <Calendar.GridHeader>
                {(day) => <Calendar.HeaderCell>{day}</Calendar.HeaderCell>}
              </Calendar.GridHeader>
              <Calendar.GridBody>{(date) => <Calendar.Cell date={date} />}</Calendar.GridBody>
            </Calendar.Grid>
          </Calendar>
        </DatePicker.Popover>
      </DatePicker>
      <Tooltip>
        <Button
          aria-label={clearLabel}
          isDisabled={!value}
          isIconOnly
          type="button"
          variant="ghost"
          onPress={onClear}
        >
          <FaIcon icon="xmark" aria-hidden="true" />
        </Button>
        <Tooltip.Content>{clearLabel}</Tooltip.Content>
      </Tooltip>
    </div>
  );
}

// TODO: Promote LocalizedDateField to @vitarish/web-ui
// Minimal bridge for clinic-web compilation.
import { useCallback, useMemo } from "react";
import { Calendar, DateField, DatePicker, Description, FieldError, Label } from "@heroui/react";
import type { DateValue } from "@internationalized/date";
import { parseIsoDateValue, dateValueToIsoDate } from "@vitarish/web-localization";

export interface LocalizedDateFieldProps {
  label: string;
  value: string;
  onChange: (value: string) => void;
  error?: string;
  description?: string;
}

export function LocalizedDateField({ label, value, onChange, error, description }: LocalizedDateFieldProps) {
  const dateValue = useMemo(() => parseIsoDateValue(value), [value]);
  const handleChange = useCallback(
    (v: DateValue | null) => onChange(dateValueToIsoDate(v)),
    [onChange]
  );

  return (
    <DatePicker>
      <Label>{label}</Label>
      <DateField value={dateValue} onChange={handleChange} />
      {description ? <Description>{description}</Description> : null}
      {error ? <FieldError>{error}</FieldError> : null}
    </DatePicker>
  );
}

export { parseIsoDateValue, dateValueToIsoDate };

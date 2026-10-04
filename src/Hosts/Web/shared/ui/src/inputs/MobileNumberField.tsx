"use client";

import { useId } from "react";

const countryCallingCode = "+98";

export interface MobileNumberFieldProps {
  /**
   * The value exactly as it will be sent to the server: raw typed text or an
   * E.164 number loaded from the server. The server owns conversion.
   */
  value: string;
  onChange: (value: string) => void;
  /** Localized field label. */
  label: string;
  /** Localized helper text shown under the input. */
  hint?: string;
  /** Localized validation error shown under the input. */
  error?: string;
  name?: string;
  placeholder?: string;
  isDisabled?: boolean;
  isRequired?: boolean;
}

function toDisplay(value: string): { text: string; showCountryPrefix: boolean } {
  if (value.startsWith(countryCallingCode))
    return { text: value.slice(countryCallingCode.length), showCountryPrefix: true };
  if (value.startsWith("+") || value.startsWith("00"))
    return { text: value, showCountryPrefix: false };
  return { text: value, showCountryPrefix: true };
}

/**
 * Iranian mobile-number input: a read-only +98 prefix with a tel-typed,
 * LTR-laid-out input that sends whatever the user typed. Conversion to the
 * stored international format is the server's job.
 */
export function MobileNumberField({
  value,
  onChange,
  label,
  hint,
  error,
  name,
  placeholder,
  isDisabled,
  isRequired,
}: MobileNumberFieldProps) {
  const inputId = useId();
  const hintId = `${inputId}-hint`;
  const errorId = `${inputId}-error`;
  const { text, showCountryPrefix } = toDisplay(value);
  const describedBy = [hint ? hintId : null, error ? errorId : null].filter(Boolean).join(" ") || undefined;

  return (
    <div dir="ltr">
      <label htmlFor={inputId} className="block">
        {label}
        <span className="mt-1 flex items-center rounded border">
          {showCountryPrefix && (
            <span className="select-none px-2 text-muted" aria-hidden="true">
              {countryCallingCode}
            </span>
          )}
          <input
            id={inputId}
            name={name}
            className="w-full bg-transparent px-2 py-2 outline-none"
            dir="ltr"
            inputMode="tel"
            autoComplete="tel-national"
            placeholder={placeholder}
            required={isRequired}
            disabled={isDisabled}
            aria-invalid={error ? true : undefined}
            aria-describedby={describedBy}
            value={text}
            onChange={(event) => onChange(event.target.value.trim())}
          />
        </span>
      </label>
      {hint && (
        <p id={hintId} className="mt-1 text-sm text-muted">
          {hint}
        </p>
      )}
      {error && (
        <p id={errorId} role="alert" className="mt-1 text-sm text-danger">
          {error}
        </p>
      )}
    </div>
  );
}

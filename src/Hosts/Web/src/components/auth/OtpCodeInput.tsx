"use client";

import { TextField, Label, Input, FieldError } from "@heroui/react";
import { useCallback } from "react";

interface OtpCodeInputProps {
  value: string;
  onChange: (value: string) => void;
  isInvalid?: boolean;
  errorMessage?: string;
  isDisabled?: boolean;
}

export function OtpCodeInput({
  value,
  onChange,
  isInvalid,
  errorMessage,
  isDisabled,
}: OtpCodeInputProps) {
  const handleChange = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>) => {
      const raw = e.target.value.replace(/[^\d۰-۹٠-٩]/g, "");
      const normalized = raw
        .replace(/[۰-۹]/g, (d) => String(d.charCodeAt(0) - 0x06f0))
        .replace(/[٠-٩]/g, (d) => String(d.charCodeAt(0) - 0x0660));
      onChange(normalized.slice(0, 6));
    },
    [onChange]
  );

  return (
    <TextField
      name="otp"
      isInvalid={isInvalid}
      isDisabled={isDisabled}
    >
      <Label>Verification Code</Label>
      <Input
        placeholder="Enter 6-digit code"
        value={value}
        onChange={handleChange}
        maxLength={6}
        pattern="[0-9]{6}"
        inputMode="numeric"
        autoComplete="one-time-code"
        className="text-center text-lg tracking-[0.5em]"
      />
      {isInvalid && errorMessage && <FieldError>{errorMessage}</FieldError>}
    </TextField>
  );
}

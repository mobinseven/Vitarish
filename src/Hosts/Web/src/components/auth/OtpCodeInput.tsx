"use client";

import { Input } from "@heroui/react";
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
    <Input
      label="Verification Code"
      placeholder="Enter 6-digit code"
      value={value}
      onChange={handleChange}
      isInvalid={isInvalid}
      errorMessage={errorMessage}
      isDisabled={isDisabled}
      maxLength={6}
      pattern="[0-9]{6}"
      inputMode="numeric"
      autoComplete="one-time-code"
      classNames={{
        input: "text-center text-lg tracking-[0.5em]",
      }}
    />
  );
}

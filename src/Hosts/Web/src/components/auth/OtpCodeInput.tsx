"use client";

import { TextField, Label, Input, FieldError } from "@heroui/react";
import { useCallback } from "react";
import { normalizeOtpCode } from "@/lib/auth/otpCode";
import { useLocalizedText } from "@/localization";

interface OtpCodeInputProps {
  value: string;
  onChange: (value: string) => void;
  isInvalid?: boolean;
  errorMessage?: string;
  isDisabled?: boolean;
  expectedLength?: number;
}

export function OtpCodeInput({
  value,
  onChange,
  isInvalid,
  errorMessage,
  isDisabled,
  expectedLength = 6,
}: OtpCodeInputProps) {
  const { t } = useLocalizedText();
  const handleChange = useCallback(
    (e: React.ChangeEvent<HTMLInputElement>) => {
      onChange(normalizeOtpCode(e.target.value, expectedLength));
    },
    [expectedLength, onChange]
  );

  return (
    <TextField
      name="otp"
      isInvalid={isInvalid}
      isDisabled={isDisabled}
    >
      <Label>{t("auth.otp.codeLabel")}</Label>
      <Input
        placeholder={t("auth.otp.codePlaceholder", { length: expectedLength })}
        value={value}
        onChange={handleChange}
        maxLength={expectedLength}
        pattern={`[0-9]{${expectedLength}}`}
        inputMode="numeric"
        autoComplete="one-time-code"
        className="text-center text-lg tracking-[0.5em]"
      />
      {isInvalid && errorMessage && <FieldError>{errorMessage}</FieldError>}
    </TextField>
  );
}

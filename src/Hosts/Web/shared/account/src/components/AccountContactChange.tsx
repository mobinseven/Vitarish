"use client";

import type { FormEvent } from "react";
import { Button, Tooltip } from "@heroui/react";
import { OtpCodeInput } from "@vitarish/auth";
import { useLocalizedText } from "@vitarish/localization";
import { FaIcon } from "@vitarish/ui";

/** The number of digits in the code the server sends; the code step stays disabled until it is complete. */
const verificationCodeLength = 6;

/** A form button with the icon and tooltip every Web button carries. */
export function ContactActionButton({
  icon,
  label,
  type = "button",
  variant,
  isPending,
  isDisabled,
  onPress,
}: {
  icon: string;
  label: string;
  type?: "button" | "submit";
  variant?: "tertiary";
  isPending?: boolean;
  isDisabled?: boolean;
  onPress?: () => void;
}) {
  return (
    <Tooltip>
      <Button type={type} variant={variant} isPending={isPending} isDisabled={isDisabled} onPress={onPress} className="w-full">
        <FaIcon icon={icon} aria-hidden="true" />
        {label}
      </Button>
      <Tooltip.Content>{label}</Tooltip.Content>
    </Tooltip>
  );
}

/** The second step of an email or phone change: the code sent to the new address or number. */
export function ContactCodeStep({
  destination,
  code,
  isLoading,
  differentLabel,
  onCodeChange,
  onSubmit,
  onReset,
}: {
  destination: string;
  code: string;
  isLoading: boolean;
  differentLabel: string;
  onCodeChange: (code: string) => void;
  onSubmit: (event: FormEvent) => void;
  onReset: () => void;
}) {
  const { t } = useLocalizedText();
  return (
    <form onSubmit={onSubmit} className="flex flex-col gap-4">
      <p className="text-sm text-muted">
        {t("auth.otp.codeSentToPrefix")}
        <span dir="ltr">{destination}</span>
        {t("auth.otp.codeSentToSuffix")}
      </p>
      <OtpCodeInput value={code} onChange={onCodeChange} isDisabled={isLoading} />
      <ContactActionButton
        type="submit"
        icon="check"
        label={t("auth.otp.verifyAction")}
        isPending={isLoading}
        isDisabled={code.length !== verificationCodeLength}
      />
      <ContactActionButton variant="tertiary" icon="rotate-left" label={differentLabel} isDisabled={isLoading} onPress={onReset} />
    </form>
  );
}

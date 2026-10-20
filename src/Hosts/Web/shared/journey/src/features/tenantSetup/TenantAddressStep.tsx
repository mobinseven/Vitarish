"use client";

import { Button, Description, FieldError, Input, InputGroup, Label, Spinner, TextField, Tooltip } from "@heroui/react";
import { useLocalizedText } from "@vitarish/localization";
import { FaIcon } from "@vitarish/ui";
import type { TenantSubdomainAvailabilityStatus } from "../../api/generated/parties/model";

interface TenantAddressStepProps {
  name: string;
  subdomain: string;
  /** `.clinic.example.com`, once a check has revealed the base domain. */
  addressSuffix: string | null;
  availabilityStatus: TenantSubdomainAvailabilityStatus | null;
  isChecking: boolean;
  checkFailed: boolean;
  canContinue: boolean;
  onNameChange: (name: string) => void;
  onSubdomainChange: (subdomain: string) => void;
  onContinue: () => void;
}

export function TenantAddressStep({
  name,
  subdomain,
  addressSuffix,
  availabilityStatus,
  isChecking,
  checkFailed,
  canContinue,
  onNameChange,
  onSubdomainChange,
  onContinue,
}: TenantAddressStepProps) {
  const { t } = useLocalizedText();
  const isUnavailable = availabilityStatus !== null && availabilityStatus !== "Available";

  return (
    <form
      className="flex flex-col gap-5"
      onSubmit={(event) => {
        event.preventDefault();
        if (canContinue) onContinue();
      }}
    >
      <TextField isRequired value={name} onChange={onNameChange}>
        <Label>{t("journey.setup.name")}</Label>
        <Input placeholder={t("journey.setup.namePlaceholder")} autoComplete="organization" />
      </TextField>

      <TextField isRequired isInvalid={isUnavailable} value={subdomain} onChange={onSubdomainChange}>
        <Label>{t("journey.setup.address")}</Label>
        <InputGroup dir="ltr">
          <InputGroup.Input placeholder={t("journey.setup.addressPlaceholder")} autoComplete="off" spellCheck={false} />
          {addressSuffix ? <InputGroup.Suffix className="text-muted">{addressSuffix}</InputGroup.Suffix> : null}
        </InputGroup>
        <Description className="text-sm">{t("journey.setup.addressRules")}</Description>
        {isUnavailable ? <FieldError className="text-sm">{t(`journey.setup.availability.${availabilityStatus}`)}</FieldError> : null}
      </TextField>

      <div aria-live="polite" className="min-h-6 text-base">
        {isChecking ? (
          <span className="inline-flex items-center gap-2 text-muted">
            <Spinner size="sm" />
            {t("journey.setup.availability.checking")}
          </span>
        ) : checkFailed ? (
          <span className="text-danger">{t("journey.setup.availability.failed")}</span>
        ) : availabilityStatus === "Available" ? (
          <span className="inline-flex items-center gap-2 text-success">
            <FaIcon icon="circle-check" aria-hidden="true" />
            {t("journey.setup.availability.Available")}
          </span>
        ) : null}
      </div>

      <div className="flex justify-end">
        <Tooltip>
          <Button type="submit" isDisabled={!canContinue}>
            {t("journey.setup.next")}
            <FaIcon icon="arrow-right" aria-hidden="true" className="rtl:-scale-x-100" />
          </Button>
          <Tooltip.Content>{t("journey.setup.nextHint")}</Tooltip.Content>
        </Tooltip>
      </div>
    </form>
  );
}

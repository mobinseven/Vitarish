"use client";

import { Alert, Button, Description, Disclosure, Label, ListBox, Select, TextArea, TextField, Tooltip } from "@heroui/react";
import { useLocalizedText } from "@vitarish/localization";
import { FaIcon } from "@vitarish/ui";
import { BusinessDetailsFields } from "./BusinessDetailsFields";
import type { BusinessDetailsForm } from "./businessDetailsForm";
import type { TenantSetupFailure } from "./useTenantSetup";

/** Mirrors the server's description limit, so typing stops at it. */
const descriptionMaximumLength = 500;

interface TenantProfileStepProps {
  categories: string[];
  category: string | null;
  description: string;
  businessDetails: BusinessDetailsForm;
  failure: TenantSetupFailure | null;
  isCreating: boolean;
  onCategoryChange: (category: string | null) => void;
  onDescriptionChange: (description: string) => void;
  onBusinessDetailsChange: (businessDetails: BusinessDetailsForm) => void;
  onBack: () => void;
  onCreate: () => void;
}

export function TenantProfileStep({
  categories,
  category,
  description,
  businessDetails,
  failure,
  isCreating,
  onCategoryChange,
  onDescriptionChange,
  onBusinessDetailsChange,
  onBack,
  onCreate,
}: TenantProfileStepProps) {
  const { t } = useLocalizedText();

  return (
    <form
      className="flex flex-col gap-5"
      onSubmit={(event) => {
        event.preventDefault();
        onCreate();
      }}
    >
      {failure ? (
        <Alert status="danger">
          <Alert.Indicator />
          <Alert.Content>
            <Alert.Description>{t(`journey.setup.failure.${failure}`)}</Alert.Description>
          </Alert.Content>
        </Alert>
      ) : null}

      <Select
        className="w-full"
        placeholder={t("journey.setup.specialtyPlaceholder")}
        value={category}
        onChange={(value) => onCategoryChange(typeof value === "string" ? value : null)}
      >
        <Label>{t("journey.setup.specialty")}</Label>
        <Select.Trigger>
          <Select.Value />
          <Select.Indicator />
        </Select.Trigger>
        <Select.Popover>
          <ListBox>
            {categories.map((code) => (
              <ListBox.Item key={code} id={code} textValue={t(`journey.setup.specialties.${code}`)}>
                {t(`journey.setup.specialties.${code}`)}
                <ListBox.ItemIndicator />
              </ListBox.Item>
            ))}
          </ListBox>
        </Select.Popover>
      </Select>

      <TextField value={description} onChange={onDescriptionChange}>
        <Label>{t("journey.setup.clinicDescription")}</Label>
        <TextArea rows={3} maxLength={descriptionMaximumLength} placeholder={t("journey.setup.clinicDescriptionPlaceholder")} />
        <Description className="text-sm">{t("journey.setup.clinicDescriptionHint")}</Description>
      </TextField>

      <Disclosure>
        <Disclosure.Heading>
          <Button slot="trigger" variant="secondary" className="w-full justify-between">
            <span className="inline-flex items-center gap-2">
              <FaIcon icon="store" aria-hidden="true" />
              {t("journey.setup.details.title")}
            </span>
            <Disclosure.Indicator />
          </Button>
        </Disclosure.Heading>
        <Disclosure.Content>
          <Disclosure.Body className="px-0 pt-5">
            <BusinessDetailsFields value={businessDetails} onChange={onBusinessDetailsChange} />
          </Disclosure.Body>
        </Disclosure.Content>
      </Disclosure>

      <div className="flex flex-col-reverse gap-3 sm:flex-row sm:justify-between">
        <Tooltip>
          <Button variant="tertiary" isDisabled={isCreating} onPress={onBack}>
            <FaIcon icon="arrow-left" aria-hidden="true" className="rtl:-scale-x-100" />
            {t("journey.setup.back")}
          </Button>
          <Tooltip.Content>{t("journey.setup.backHint")}</Tooltip.Content>
        </Tooltip>
        <Tooltip>
          <Button type="submit" isPending={isCreating}>
            <FaIcon icon="hospital" aria-hidden="true" />
            {t("journey.setup.create")}
          </Button>
          <Tooltip.Content>{t("journey.setup.createHint")}</Tooltip.Content>
        </Tooltip>
      </div>
    </form>
  );
}
